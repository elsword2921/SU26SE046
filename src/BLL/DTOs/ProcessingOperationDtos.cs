namespace BLL.DTOs;

public record CreateProcessingOperationDto(
    string OperationType,
    Guid WarehouseId,
    Guid OrganizationId,
    string? RequestNotes,
    List<CreateProcessingOperationInputDto> Inputs);

public record CreateProcessingOperationInputDto(
    Guid InventoryId,
    int RequestedQuantity,
    decimal RequestedWeight);

public record ProcessingOperationCreatedDto(
    Guid Id,
    string OperationCode,
    string OperationType,
    string Status);

public record ProcessingOperationDecisionDto(
    string? RejectionReason);

public record ProcessingOperationListDto(
    Guid Id,
    string OperationCode,
    string OperationType,
    string Status,
    Guid WarehouseId,
    string WarehouseName,
    Guid OrganizationId,
    string OrganizationName,
    DateTime RequestedAt,
    int InputCount,
    int TotalRequestedQuantity,
    decimal TotalRequestedWeight);

public record ProcessingOperationDetailDto(
    Guid Id,
    string OperationCode,
    string OperationType,
    string Status,
    Guid WarehouseId,
    string WarehouseName,
    Guid OrganizationId,
    string OrganizationName,
    Guid? CreatedByUserId,
    Guid? ApprovedByOrganizationId,
    Guid? ApprovedByManagerId,
    Guid? IssuedByStaffId,
    DateTime RequestedAt,
    DateTime? OrganizationRespondedAt,
    DateTime? ManagerRespondedAt,
    DateTime? ApprovedAt,
    DateTime? IssuedAt,
    DateTime? OrganizationReceivedAt,
    DateTime? ProcessingStartedAt,
    DateTime? ProcessingCompletedAt,
    DateTime? OutputReturnedAt,
    string? TrackingCode,
    string? CarrierName,
    string? RequestNotes,
    string? OrganizationRejectionReason,
    string? ManagerRejectionReason,
    string? CompletionNotes,
    List<ProcessingOperationInputDetailDto> Inputs,
    List<ProcessingOperationOutputDetailDto> Outputs);

public record ProcessingOperationInputDetailDto(
    Guid Id,
    Guid InventoryId,
    string InventorySku,
    Guid? ClassifiedBatchId,
    string? ClassifiedBatchCode,
    int RequestedQuantity,
    decimal RequestedWeight,
    int IssuedQuantity,
    decimal IssuedWeight,
    string? Notes);

public record ProcessingOperationOutputDetailDto(
    Guid Id,
    string OutputType,
    int Quantity,
    decimal Weight,
    int ReturnedQuantity,
    decimal ReturnedWeight,
    Guid? RecordedByStaffId,
    DateTime? RecordedAt,
    string? Notes);

public record IssueProcessingOperationDto(
    string? Notes);