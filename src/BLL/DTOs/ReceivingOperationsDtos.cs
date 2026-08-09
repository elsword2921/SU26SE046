namespace BLL.DTOs;

public record GenerateShiftsDto(Guid WarehouseId, DateTime Date);
public record GenerateYearShiftsDto(Guid WarehouseId, int Year, List<DateTime>? HolidayDates,
    List<DayOfWeek>? WorkingDays, TimeSpan? MorningStartTime, TimeSpan? MorningEndTime,
    TimeSpan? AfternoonStartTime, TimeSpan? AfternoonEndTime);
public record GenerateYearShiftsResultDto(int WorkingDays, int CreatedShifts, int SkippedExisting);
public record GenerateMonthShiftsDto(
    Guid WarehouseId,
    int Year,
    int Month,
    List<DateTime>? HolidayDates,
    List<DayOfWeek>? WorkingDays,
    TimeSpan? MorningStartTime,
    TimeSpan? MorningEndTime,
    TimeSpan? AfternoonStartTime,
    TimeSpan? AfternoonEndTime);
public record GenerateMonthShiftsResultDto(
    int WorkingDays,
    int CreatedShifts,
    int SkippedExisting);
public record DeleteYearShiftsDto(Guid WarehouseId, int Year);
public record DeleteYearShiftsResultDto(int DeletedShifts, int SkippedOperationalShifts);
public record UpdateManagerShiftDto(Guid WarehouseId, string ShiftName, DateTime ShiftDate,
    TimeSpan StartTime, TimeSpan EndTime);
public record CreateReceivingTeamDto(Guid ShiftId, string TeamName, List<Guid> StaffIds,
    string TeamType = "ReceivingPickup");
public record UpdateReceivingTeamDto(string TeamName, List<Guid> StaffIds);
public record PlanReceivingShiftDto(Guid ShiftId, Guid TeamId);
public record AutoBalanceShiftDto(Guid ShiftId);
public record AutoBalanceResultDto(int TeamCount, int RequestCount,
    Dictionary<Guid, int> RequestsPerTeam);
public record AssignDonationRequestDto(Guid RequestId, Guid TeamId);
public record ConfirmPickupDto(decimal ActualWeight, string? Notes, List<string>? ImageUrls);
public record ReschedulePickupDto(DateTime PickupDate, string? Reason);
public record RejectPickupDto(string Reason);
public record WarehouseDutyContextDto(Guid TeamId, string TeamName, Guid ShiftId, string ShiftName,
    DateTime ShiftDate, TimeSpan StartTime, TimeSpan EndTime, string ShiftStatus,
    Guid WarehouseId, string WarehouseName, string WarehouseAddress, Guid? IntakeBatchId);
public record WarehouseDropOffItemDto(Guid Id, Guid WarehouseId, string Code, string ContactName, string PhoneNumber,
    string Address, DateTime ExpectedDate, string Description, decimal EstimateWeight,
    string Status, List<string>? ImageUrls);
public record WarehouseDropOffBoardDto(List<WarehouseDutyContextDto> DutyContexts,
    List<WarehouseDropOffItemDto> Requests);

public class ReceivingBatchDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public Guid ShiftId { get; set; }
    public string ShiftStatus { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string WarehouseAddress { get; set; } = string.Empty;
    public List<ReceivingTeamMemberDto> TeamMembers { get; set; } = [];
    public List<ReceivingRequestDto> Requests { get; set; } = [];
}

public class ReceivingRequestDto
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimateWeight { get; set; }
    public decimal? ActualWeight { get; set; }
    public DateTime? PickupDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string DeliveryMethod { get; set; } = string.Empty;
    public List<string>? ImageUrls { get; set; }
}

public record ReceivingTeamMemberDto(Guid Id, string FullName, string PhoneNumber);
public record DispatchRequestDto(Guid Id, string Code, string ContactName, string PhoneNumber,
    string DeliveryMethod, string Address, DateTime? ScheduledDate, Guid WarehouseId, string WarehouseName,
    DateTime? CreatedAt);
public record DispatchTeamDto(Guid Id, string TeamName, string TeamType, Guid ShiftId, string ShiftName,
    DateTime ShiftDate, string ShiftTime, Guid WarehouseId, List<ReceivingTeamMemberDto> Members);
public record ReceivingDispatchBoardDto(List<DispatchRequestDto> Requests, List<DispatchTeamDto> Teams);
public record ManagerWarehouseOptionDto(Guid Id, string Name, string Address);
public record ManagerStaffOptionDto(Guid Id, string FullName, string UserName, string PhoneNumber,
    Guid? WarehouseId);
public record ManagerAssignedRequestDto(Guid Id, string Code, string ContactName, string PhoneNumber,
    string Address, DateTime? PickupDate, string DeliveryMethod, string Status, int RouteOrder);
public record ManagerTeamOverviewDto(Guid Id, string TeamName, string TeamType,
    List<ReceivingTeamMemberDto> Members,
    Guid? IntakeBatchId, string? IntakeBatchCode, string? IntakeBatchStatus,
    string? IntakeBatchRoute, decimal IntakeBatchWeight,
    List<ManagerAssignedRequestDto> Requests);
public record ManagerShiftOverviewDto(Guid Id, Guid WarehouseId, string WarehouseName, string ShiftName,
    DateTime ShiftDate, TimeSpan StartTime, TimeSpan EndTime, string Status,
    List<ManagerTeamOverviewDto> Teams, int AssignedRequests, int PendingDropOffRequests);
public record ManagerReceivingSetupDto(List<ManagerWarehouseOptionDto> Warehouses,
    List<ManagerStaffOptionDto> ReceivingStaff, List<ManagerShiftOverviewDto> Shifts);
public enum ShiftGenerationPeriodUnit
{
    Day, Week, Month, Quarter, Year, Custom
}

public record ShiftDefinitionDto(string Name, TimeSpan StartTime, TimeSpan EndTime);

public record GenerateShiftsV2Dto(Guid WarehouseId, DateTime StartDate, ShiftGenerationPeriodUnit PeriodUnit, int PeriodValue,
    DateTime? CustomEndDate, List<DayOfWeek>? WorkingDays, List<DateTime>? ExcludedDates, List<ShiftDefinitionDto>? ShiftDefinitions);

public record GenerateShiftsResultDto(DateTime StartDate, DateTime EndDate, int WorkingDays, int CreatedShifts, int SkippedExisting);