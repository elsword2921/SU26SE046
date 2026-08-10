using BLL.DTOs;
using BLL.Common;
using BLL.Services.Interfaces.DonorRequestService;
using BLL.Services.Implements.Notifications;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.DonorRequestService
{
    public class DonorRequestService : IDonorRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public DonorRequestService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }
        public async Task<Guid> CreateAsync(
            Guid donorId,
            CreateDonorRequestDto dto)
        {
            var warehouse =
                await _unitOfWork
                .WarehouseRepository
                .GetByIdAsync(dto.WarehouseId);

            if (warehouse == null)
            {
                throw new Exception(
                    "Warehouse not found");
            }

            var deliveryMethod = dto.DeliveryMethod?.Trim() switch
            {
                "StaffPickup" => "StaffPickup",
                "DonorDropOff" => "DonorDropOff",
                _ => throw new InvalidOperationException("Delivery method must be StaffPickup or DonorDropOff.")
            };
            var contactName = dto.ContactName?.Trim();
            var contactPhone = new string((dto.ContactPhoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(contactName))
                throw new InvalidOperationException("Contact name is required.");
            if (contactPhone.Length != 10 || contactPhone[0] != '0')
                throw new InvalidOperationException("A valid 10-digit Vietnamese contact phone number is required.");
            if (deliveryMethod == "StaffPickup" &&
                (string.IsNullOrWhiteSpace(dto.PickupAddress) || !dto.PickupDate.HasValue))
                throw new InvalidOperationException("Pickup address and pickup date are required for staff pickup.");
            if (dto.PickupDate.HasValue && dto.PickupDate.Value.Date < GetEarliestPickupDate())
                throw new InvalidOperationException(
                    "Ngày tiếp nhận không hợp lệ. Từ 11:00, ngày sớm nhất có thể chọn là ngày mai.");
            if (dto.PickupDate.HasValue && IsWeekend(dto.PickupDate.Value))
                throw new InvalidOperationException(
                    "Hệ thống chỉ tiếp nhận quyên góp từ Thứ Hai đến Thứ Sáu.");

            var requestId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var request =
                new DonationRequest
                {
                    Id = requestId,
                    RequestCode = BuildRequestCode(requestId, now),
                    DonorId = donorId,
                    WarehouseId = dto.WarehouseId,
                    ContactName = contactName,
                    ContactPhoneNumber = contactPhone,
                    DeliveryMethod = deliveryMethod,
                    PickupDate = dto.PickupDate.HasValue
                        ? DateTime.SpecifyKind(dto.PickupDate.Value, DateTimeKind.Unspecified)
                        : null,
                    Description = dto.Description,
                    ImageUrls = dto.ImageUrls,
                    EstimateWeight = dto.EstimateWeight,
                    PickupAddress = deliveryMethod == "StaffPickup"
                        ? dto.PickupAddress!.Trim()
                        : warehouse.Address,
                    CreateAt = now,
                    Status = deliveryMethod == "StaffPickup"
                        ? DonationRequestStatus.WaitingReceivingStaff
                        : DonationRequestStatus.PendingStaffAssign
                };

            await _unitOfWork
                .DonorRequestRepository
                .AddAsync(request);

            await NotificationWriter.NotifyManagersNewRequestAsync(_context, request);
            await _unitOfWork.SaveChangeAsync();
            return requestId;
        }

        private static DateTime GetEarliestPickupDate()
        {
            var vietnamNow = VietnamTime.Now;
            var currentMinutes = vietnamNow.Hour * 60 + vietnamNow.Minute;
            const int cutoffMinutes = 11 * 60;
            var earliest = vietnamNow.Date.AddDays(currentMinutes >= cutoffMinutes ? 1 : 0);
            while (IsWeekend(earliest)) earliest = earliest.AddDays(1);
            return earliest;
        }

        private static bool IsWeekend(DateTime date) =>
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        public async Task UpdateAsync(Guid donorId, Guid requestId, UpdateDonorRequestDto dto)
        {
            var request =
                await _unitOfWork
                .DonorRequestRepository
                .GetWithConditionAsync(
                    x => x.Id == requestId
                         && x.DonorId == donorId
                         && x.IsActive != false);

            if (request == null)
            {
                throw new Exception("Donation request not found");
            }

            if (!CanDonorModify(request.Status))
            {
                throw new Exception("Donation request cannot be updated at this status");
            }

            var warehouse =
                await _unitOfWork
                .WarehouseRepository
                .GetByIdAsync(dto.WarehouseId);

            if (warehouse == null)
            {
                throw new Exception("Warehouse not found");
            }

            if (dto.PickupDate.Date < GetEarliestPickupDate())
                throw new InvalidOperationException(
                    "Ngày tiếp nhận không hợp lệ. Từ 11:00, ngày sớm nhất có thể chọn là ngày mai.");
            if (IsWeekend(dto.PickupDate))
                throw new InvalidOperationException(
                    "Hệ thống chỉ tiếp nhận quyên góp từ Thứ Hai đến Thứ Sáu.");

            request.WarehouseId = dto.WarehouseId;
            request.PickupDate = DateTime.SpecifyKind(dto.PickupDate, DateTimeKind.Unspecified);
            request.Description = dto.Description;
            request.ImageUrls = dto.ImageUrls;
            request.EstimateWeight = dto.EstimateWeight;
            request.PickupAddress = dto.PickupAddress;
            request.UpdateAt = DateTime.UtcNow;

            await _unitOfWork
                .DonorRequestRepository
                .UpdateAsync(request);

            await _unitOfWork.SaveChangeAsync();
        }

        public async Task CancelAsync(Guid donorId, Guid requestId)
        {
            var request =
                await _unitOfWork
                .DonorRequestRepository
                .GetWithConditionAsync(
                    x => x.Id == requestId
                         && x.DonorId == donorId
                         && x.IsActive != false);

            if (request == null)
            {
                throw new Exception("Donation request not found");
            }

            if (!CanDonorModify(request.Status))
            {
                throw new Exception("Donation request cannot be cancelled at this status");
            }

            request.Status = DonationRequestStatus.Cancelled;
            request.RejectReason = "Cancelled by donor";
            request.UpdateAt = DateTime.UtcNow;

            await _unitOfWork
                .DonorRequestRepository
                .UpdateAsync(request);

            await _unitOfWork.SaveChangeAsync();
        }
        public async Task<List<DonorRequestSearchResultDto>> SearchByPhoneNumberAsync(string phoneNumber)
        {
            var normalizedPhoneNumber =
                new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(normalizedPhoneNumber))
            {
                return new List<DonorRequestSearchResultDto>();
            }

            var requests =
                await _unitOfWork
                .DonorRequestRepository
                .GetAllAsync(
                    x => x.ContactPhoneNumber == normalizedPhoneNumber
                         && x.IsActive != false,
                    noTracked: true);

            return await MapToSearchResult(requests)
                .ToListAsync();

        }

        public async Task<List<DonorRequestSearchResultDto>> GetByDonorIdAsync(Guid donorId)
        {
            var requests =
                await _unitOfWork
                .DonorRequestRepository
                .GetAllAsync(
                    x => x.DonorId == donorId
                         && x.IsActive != false,
                    noTracked: true);

            return await MapToSearchResult(requests)
                .ToListAsync();
        }

        private static IQueryable<DonorRequestSearchResultDto> MapToSearchResult(IQueryable<DonationRequest> requests)
        {
            return requests
                .Include(x => x.Donor)
                .Include(x => x.Warehouse)
                .OrderByDescending(x => x.CreateAt)
                .Select(x => new DonorRequestSearchResultDto
                {
                    Id = x.Id,
                    Code = x.RequestCode,
                    DonorName = x.ContactName,
                    PhoneNumber = x.ContactPhoneNumber,
                    DeliveryMethod = x.DeliveryMethod,
                    Description = x.Description,
                    ImageUrls = x.ImageUrls,
                    EstimateWeight = x.EstimateWeight,
                    ActualWeight = x.ActualWeight,
                    PickupAddress = x.PickupAddress,
                    PickupDate = x.PickupDate,
                    WarehouseId = x.WarehouseId,
                    WarehouseAddress = x.Warehouse.Address,
                    Status = x.Status.ToString(),
                    StatusText = x.DeliveryMethod == "DonorDropOff"
                        && x.Status == DonationRequestStatus.PendingStaffAssign
                            ? "Chờ người quyên góp mang hàng đến kho"
                            : GetStatusText(x.Status),
                    CreatedAt = x.CreateAt,
                });
        }
        private static bool CanDonorModify(DonationRequestStatus status)
        {
            return status == DonationRequestStatus.PendingStaffAssign
                   || status == DonationRequestStatus.WaitingReceivingStaff;
        }

        private static string BuildRequestCode(Guid id, DateTime createdAt) =>
            $"DR-{createdAt.Year}-{id.ToString("N")[..8].ToUpperInvariant()}";

        private static string GetStatusText(DonationRequestStatus status)
        {
            return status switch
            {
                DonationRequestStatus.PendingStaffAssign => "Đang chờ phân công nhân viên",
                DonationRequestStatus.ReceivingStaffAssigned => "Đã phân công nhân viên tiếp nhận",
                DonationRequestStatus.WaitingReceivingStaff => "Đang chờ phân công nhân viên tiếp nhận",
                DonationRequestStatus.Confirmed => "Đã xác nhận đơn quyên góp",
                DonationRequestStatus.Reject => "Đơn quyên góp bị từ chối",
                DonationRequestStatus.SendToClassification => "Đã chuyển sang phân loại",
                DonationRequestStatus.Classifying => "Đang phân loại",
                DonationRequestStatus.Classified => "Đã phân loại",
                DonationRequestStatus.Stored => "Đã lưu kho",
                DonationRequestStatus.Cancelled => "Đơn quyên góp bị hủy",
                _ => "Đang xử lý",
            };
        }  
    }
}
