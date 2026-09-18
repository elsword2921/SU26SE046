namespace BLL.DTOs;

public record ReceivingLimitsDto(int MaxRequests, decimal MaxWeightKg);
public record ReceivingWarehouseLimitsDto(Guid Id, string Name, int MaxRequests, decimal MaxWeightKg);
public record ReceivingTeamLoadDto(Guid Id, string TeamName, Guid WarehouseId, Guid ShiftId,
    string ShiftName, DateTime ShiftDate, TimeSpan StartTime, TimeSpan EndTime, string Status,
    string TeamType, int MaxRequests, decimal MaxWeightKg, int AssignedRequests,
    decimal EstimatedWeightKg, decimal ActualWeightKg, bool UsesWarehouseDefaults);
public record ReceivingCapacityBoardDto(List<ReceivingWarehouseLimitsDto> Warehouses, List<ReceivingTeamLoadDto> Teams);
public record ReceivingSuggestedAssignmentDto(Guid RequestId, string Code, string Address, decimal EstimateWeight,
    Guid TeamId, List<Guid> EligibleTeamIds);
public record ReceivingUnassignedDto(Guid RequestId, string Code, decimal EstimateWeight, string Reason);
public record ReceivingPlanPreviewDto(Guid ShiftId, List<ReceivingTeamLoadDto> Teams,
    List<ReceivingSuggestedAssignmentDto> Assignments, List<ReceivingUnassignedDto> Unassigned);
public record ApplyReceivingPlanDto(Guid ShiftId, List<AssignDonationRequestDto> Assignments);
