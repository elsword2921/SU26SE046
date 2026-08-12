using BLL.DTOs;
using BLL.Common;
using BLL.Services.Interfaces.DonorRequestService;
using BLL.Services.Implements.Notifications;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BLL.Services.Implements.DonorRequestService
{
    public class DonorRequestService : IDonorRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly HttpClient _geocodingClient;

        public DonorRequestService(
            IUnitOfWork unitOfWork,
            AppDbContext context,
            HttpClient geocodingClient)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _geocodingClient = geocodingClient;
        }
        public async Task<Guid> CreateAsync(
            Guid donorId,
            CreateDonorRequestDto dto)
        {
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
            if (!dto.PickupDate.HasValue)
                throw new InvalidOperationException("Ngày và giờ tiếp nhận là bắt buộc.");
            if (deliveryMethod == "StaffPickup" && string.IsNullOrWhiteSpace(dto.PickupAddress))
                throw new InvalidOperationException("Địa chỉ lấy hàng là bắt buộc.");
            if (deliveryMethod == "StaffPickup"
                && (!dto.PickupLatitude.HasValue || !dto.PickupLongitude.HasValue))
                throw new InvalidOperationException("Vui lòng chọn một địa chỉ hợp lệ trên bản đồ.");
            if (deliveryMethod == "DonorDropOff" && !dto.WarehouseId.HasValue)
                throw new InvalidOperationException("Vui lòng chọn kho tiếp nhận.");
            if (dto.PickupDate.HasValue && dto.PickupDate.Value <= VietnamTime.Now)
                throw new InvalidOperationException("Khung giờ tiếp nhận phải nằm trong tương lai.");
            if (dto.PickupDate.HasValue && IsWeekend(dto.PickupDate.Value))
                throw new InvalidOperationException(
                    "Hệ thống chỉ tiếp nhận quyên góp từ Thứ Hai đến Thứ Sáu.");

            var warehouse = deliveryMethod == "DonorDropOff"
                ? await _context.Warehouses.FirstOrDefaultAsync(x =>
                    x.Id == dto.WarehouseId && x.IsActive != false)
                : await ResolveNearestWarehouseAsync(
                    dto.PickupLatitude!.Value,
                    dto.PickupLongitude!.Value);
            if (warehouse is null)
                throw new InvalidOperationException("Kho tiếp nhận không tồn tại hoặc đã ngừng hoạt động.");
            if (!dto.PickupDate.HasValue)
                throw new InvalidOperationException("Ngày và khung giờ tiếp nhận là bắt buộc.");
            ValidateBusinessHours(dto.PickupDate.Value);

            var requestId = Guid.NewGuid();
            var now = VietnamTime.Now;
            var request =
                new DonationRequest
                {
                    Id = requestId,
                    RequestCode = BuildRequestCode(requestId, now),
                    DonorId = donorId,
                    WarehouseId = warehouse.Id,
                    ContactName = contactName,
                    ContactPhoneNumber = contactPhone,
                    DeliveryMethod = deliveryMethod,
                    PickupDate = dto.PickupDate.HasValue
                        ? DateTime.SpecifyKind(dto.PickupDate.Value, DateTimeKind.Unspecified)
                        : null,
                    Description = dto.Description,
                    ImageUrls = dto.ImageUrls,
                    EstimateWeight = dto.EstimateWeight,
                    PickupAddress = deliveryMethod == "DonorDropOff"
                        ? warehouse.Address
                        : dto.PickupAddress!.Trim(),
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
            var earliest = VietnamTime.Today;
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

            var warehouse = dto.PickupLatitude.HasValue && dto.PickupLongitude.HasValue
                ? await ResolveNearestWarehouseAsync(
                    dto.PickupLatitude.Value,
                    dto.PickupLongitude.Value)
                : await _context.Warehouses.FirstAsync(x => x.Id == request.WarehouseId);

            if (dto.PickupDate.Date < VietnamTime.Today)
                throw new InvalidOperationException("Ngày tiếp nhận không được nằm trong quá khứ.");
            if (IsWeekend(dto.PickupDate))
                throw new InvalidOperationException(
                    "Hệ thống chỉ tiếp nhận quyên góp từ Thứ Hai đến Thứ Sáu.");

            ValidateBusinessHours(dto.PickupDate);
            request.WarehouseId = warehouse.Id;
            request.PickupDate = DateTime.SpecifyKind(dto.PickupDate, DateTimeKind.Unspecified);
            request.Description = dto.Description;
            request.ImageUrls = dto.ImageUrls;
            request.EstimateWeight = dto.EstimateWeight;
            request.PickupAddress = dto.PickupAddress;
            request.UpdateAt = VietnamTime.Now;

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

        public async Task<DonorPickupAvailabilityDto> GetPickupAvailabilityAsync(
            DateTime date,
            double latitude,
            double longitude)
        {
            if (IsWeekend(date))
                return new DonorPickupAvailabilityDto(Guid.Empty, []);
            var warehouse = await ResolveNearestWarehouseAsync(latitude, longitude, date);
            var shifts = await _context.Shifts.AsNoTracking()
                .Where(x => x.IsActive != false
                    && x.Status == "Scheduled"
                    && x.WarehouseId == warehouse.Id
                    && x.ShiftDate.Date == date.Date)
                .OrderBy(x => x.StartTime)
                .Select(x => new { x.Id, x.ShiftName, x.StartTime, x.EndTime })
                .ToListAsync();
            var now = VietnamTime.Now;
            var windows = shifts
                .Where(x => date.Date.Add(x.EndTime) > now)
                .Select(x => new DonorPickupWindowDto(
                x.Id,
                x.ShiftName,
                x.StartTime,
                x.EndTime,
                $"{x.StartTime:hh\\:mm} - {x.EndTime:hh\\:mm}"))
                .ToList();
            return new DonorPickupAvailabilityDto(warehouse.Id, windows);
        }

        private async Task ValidatePickupWindowAsync(Guid warehouseId, DateTime pickupDateTime)
        {
            var pickupTime = pickupDateTime.TimeOfDay;
            var valid = await _context.Shifts.AsNoTracking().AnyAsync(x =>
                x.IsActive != false
                && x.Status == "Scheduled"
                && x.WarehouseId == warehouseId
                && x.ShiftDate.Date == pickupDateTime.Date
                && x.StartTime <= pickupTime
                && pickupTime < x.EndTime);
            if (!valid)
                throw new InvalidOperationException(
                    "Khung giờ tiếp nhận không còn khả dụng tại kho gần nhất. Vui lòng chọn lại.");
        }

        private static void ValidateBusinessHours(DateTime pickupDateTime)
        {
            var pickupTime = pickupDateTime.TimeOfDay;
            if (pickupTime < new TimeSpan(8, 0, 0) || pickupTime > new TimeSpan(17, 0, 0))
                throw new InvalidOperationException(
                    "Giờ tiếp nhận phải nằm trong khoảng từ 08:00 đến 17:00.");
        }

        private async Task<Warehouse> ResolveNearestWarehouseAsync(
            double latitude,
            double longitude,
            DateTime? serviceDate = null)
        {
            if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
                throw new InvalidOperationException("Tọa độ địa chỉ không hợp lệ.");
            var warehouses = await _context.Warehouses
                .Where(x => x.IsActive != false)
                .ToListAsync();

            if (serviceDate.HasValue)
            {
                var now = VietnamTime.Now;
                var scheduledShifts = await _context.Shifts.AsNoTracking()
                    .Where(x => x.IsActive != false
                        && x.Status == "Scheduled"
                        && x.ShiftDate.Date == serviceDate.Value.Date)
                    .Select(x => new { x.WarehouseId, x.ShiftDate, x.EndTime })
                    .ToListAsync();
                var availableWarehouseIds = scheduledShifts
                    .Where(x => x.ShiftDate.Date.Add(x.EndTime) > now)
                    .Select(x => x.WarehouseId)
                    .Distinct()
                    .ToList();
                warehouses = warehouses
                    .Where(x => availableWarehouseIds.Contains(x.Id))
                    .ToList();
            }
            if (warehouses.Count == 0)
                throw new InvalidOperationException(serviceDate.HasValue
                    ? "Ngày đã chọn chưa có kho nào mở ca tiếp nhận."
                    : "Hiện chưa có kho tiếp nhận đang hoạt động.");

            var coordinatesUpdated = false;
            foreach (var warehouse in warehouses.Where(x => !x.Latitude.HasValue || !x.Longitude.HasValue))
            {
                var coordinate = await GeocodeAsync(warehouse.Address);
                if (coordinate is null) continue;
                warehouse.Latitude = coordinate.Value.Latitude;
                warehouse.Longitude = coordinate.Value.Longitude;
                warehouse.UpdateAt = VietnamTime.Now;
                coordinatesUpdated = true;
            }
            if (coordinatesUpdated) await _context.SaveChangesAsync();

            var located = warehouses.Where(x => x.Latitude.HasValue && x.Longitude.HasValue).ToList();
            if (located.Count == 0)
                throw new InvalidOperationException(
                    "Không xác định được tọa độ các kho. Manager cần cập nhật địa chỉ kho hợp lệ.");
            return located.MinBy(x => DistanceKm(
                latitude, longitude, x.Latitude!.Value, x.Longitude!.Value))!;
        }

        private async Task<(double Latitude, double Longitude)?> GeocodeAsync(string address)
        {
            var url = $"search?format=jsonv2&limit=1&countrycodes=vn&q={Uri.EscapeDataString(address)}";
            using var response = await _geocodingClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var first = document.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined) return null;
            if (!double.TryParse(first.GetProperty("lat").GetString(),
                    System.Globalization.CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(first.GetProperty("lon").GetString(),
                    System.Globalization.CultureInfo.InvariantCulture, out var lon)) return null;
            return (lat, lon);
        }

        private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double radius = 6371;
            static double ToRadians(double value) => value * Math.PI / 180;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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
