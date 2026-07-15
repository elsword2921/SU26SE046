using BLL.DTOs;
using BLL.Services.Interfaces.DonorRequestService;
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
        public async Task CreateAsync(
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

            var request =
                new DonationRequest
                {
                    Id = Guid.NewGuid(),
                    DonorId = donorId,
                    WarehouseId = dto.WarehouseId,
                    PickupDate = DateTime.SpecifyKind(dto.PickupDate, DateTimeKind.Utc),
                    Description = dto.Description,
                    ImageUrls = dto.ImageUrls,
                    EstimateWeight = dto.EstimateWeight,
                    PickupAddress = dto.PickupAddress,
                    CreateAt = DateTime.UtcNow,
                    Status = DonationRequestStatus.WaitingReceivingStaff
                };

            await _unitOfWork
                .DonorRequestRepository
                .AddAsync(request);

            await AssignToAvailableReceivingBatchAsync(request);
            await _unitOfWork.SaveChangeAsync();
        }

        private async Task AssignToAvailableReceivingBatchAsync(DonationRequest request)
        {
            var pickupDate = request.PickupDate!.Value.Date;
            var today = DateTime.UtcNow.Date;
            if (pickupDate > today) return;

            var batch = await _context.IntakeBatches
                .Include(x => x.Shift)
                .Where(x => x.WarehouseId == request.WarehouseId
                    && x.IsActive != false
                    && x.ReceivingTeamId.HasValue
                    && (x.Status == "Planned" || x.Status == "Receiving"
                        || (x.Status == "Completed" && x.Shift.ShiftDate.Date == today))
                    && (x.Shift.Status == "Scheduled" || x.Shift.Status == "InProgress"
                        || (x.Shift.Status == "Completed" && x.Shift.ShiftDate.Date == today))
                    && x.Shift.ShiftDate.Date >= pickupDate
                    && x.Shift.ShiftDate.Date <= today)
                .OrderByDescending(x => x.Shift.ShiftDate)
                .ThenByDescending(x => x.Shift.StartTime)
                .FirstOrDefaultAsync();

            if (batch is null) return;

            if (batch.Status == "Completed")
            {
                batch.Status = "Planned";
                batch.CompletedAt = null;
                batch.UpdateAt = DateTime.UtcNow;
                batch.Shift.Status = "Scheduled";
                batch.Shift.StartedAt = null;
                batch.Shift.CompletedAt = null;
                batch.Shift.UpdateAt = DateTime.UtcNow;
            }

            var lastRouteOrder = await _context.PickupAssignments
                .Where(x => x.IntakeBatchId == batch.Id && x.IsActive != false)
                .Select(x => (int?)x.RouteOrder)
                .MaxAsync() ?? 0;

            _context.PickupAssignments.Add(new PickupAssignment
            {
                Id = Guid.NewGuid(),
                DonorRequestId = request.Id,
                ShiftId = batch.ShiftId,
                TeamId = batch.ReceivingTeamId!.Value,
                IntakeBatchId = batch.Id,
                RouteOrder = lastRouteOrder + 1,
                AreaKey = request.PickupAddress,
                Status = "Pending",
                CreateAt = DateTime.UtcNow
            });
            request.Status = DonationRequestStatus.ReceivingStaffAssigned;
            request.UpdateAt = DateTime.UtcNow;
        }
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

            request.WarehouseId = dto.WarehouseId;
            request.PickupDate = DateTime.SpecifyKind(dto.PickupDate, DateTimeKind.Utc);
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
                    x => x.Donor.PhoneNumber == normalizedPhoneNumber
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
                    Code = "DR-" + x.CreateAt.GetValueOrDefault().Year + "-" + x.Id.ToString().Substring(0, 8).ToUpper(),
                    DonorName = x.Donor.FullName,
                    PhoneNumber = x.Donor.PhoneNumber,
                    Description = x.Description,
                    ImageUrls = x.ImageUrls,
                    EstimateWeight = x.EstimateWeight,
                    ActualWeight = x.ActualWeight,
                    PickupAddress = x.PickupAddress,
                    PickupDate = x.PickupDate,
                    WarehouseId = x.WarehouseId,
                    WarehouseAddress = x.Warehouse.Address,
                    Status = x.Status.ToString(),
                    StatusText = GetStatusText(x.Status),
                    CreatedAt = x.CreateAt,
                });
        }
        private static bool CanDonorModify(DonationRequestStatus status)
        {
            return status == DonationRequestStatus.PendingStaffAssign
                   || status == DonationRequestStatus.WaitingReceivingStaff;
        }

        private static string GetStatusText(DonationRequestStatus status)
        {
            return status switch
            {
                DonationRequestStatus.PendingStaffAssign => "Đang chờ phân công nhân viên",
                DonationRequestStatus.ReceivingStaffAssigned => "Đã phân công nhân viên tiếp nhận",
                DonationRequestStatus.WaitingReceivingStaff => "Đang chờ nhân viên tiếp nhận đến lấy",
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
