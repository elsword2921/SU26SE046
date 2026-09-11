namespace BLL.DTOs;

public record CreateProcessingOperationDto(
    string OperationType,
    Guid WarehouseId,
    Guid OrganizationId,
    string? RequestNotes,
    List<CreateProcessingOperationInputDto> Inputs);

public record CreateProcessingOperationInputDto(
    Guid InventoryId,
    int? RequestedQuantity = null,
    decimal? RequestedWeight = null);

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
    List<ProcessingOperationOutputDetailDto> Outputs)
{
    public ProcessingShippingDto? Shipping { get; init; }
    public RecyclingReturnDetailDto? RecyclingReturn { get; init; }
}

public record ScheduleRecyclingReturnDto(DateTime ExpectedReturnDate, string? Notes);
public record DispatchRecyclingReturnDto(string CarrierName, string TrackingCode, string CompletionNotes,
    List<RecyclingOutputDto> Batches);
public record RecyclingOutputDto(string Description, int Quantity, decimal Weight);
public record ReceiveRecyclingReturnDto(Guid ShiftId, List<ReceiveRecyclingBatchDto> Batches);
public record ReceiveRecyclingBatchDto(Guid OutputId, Guid StorageLocationId, int Quantity, decimal Weight, string? Notes);
public record RecyclingReturnDetailDto(DateTime? ExpectedReturnDate, DateTime? DispatchedAt,
    DateTime? ReceivedAt, string? CarrierName, string? TrackingCode, string? Notes,
    List<RecyclingReturnBatchDto> Batches);
public record RecyclingReturnBatchDto(Guid OutputId, string Description, int Quantity, decimal Weight,
    int ReceivedQuantity, decimal ReceivedWeight, string? ReceiptNotes, Guid? IntakeBatchId,
    string? BatchCode, string? Status, string? AreaName, string? LocationCode, string? ClassificationTeamName);
public record RecyclingReceiptOptionsDto(List<RecyclingReceiptShiftDto> Shifts, List<RecyclingReceiptLocationDto> Locations);
public record RecyclingReceiptShiftDto(Guid Id, string Name, DateTime Date);
public record RecyclingReceiptLocationDto(Guid Id, string Code, string AreaName, decimal AvailableKg);

public record ProcessingShippingDto(string WarehouseAddress, string? WarehousePhone,
    string RecipientPhone, string RecipientAddress, string? GhnOrderCode, string? GhnStatus,
    DateTime? GhnUpdatedAt, List<ShipmentEventDto> History);

public record CreateProcessingGhnShipmentDto(
    int PaymentTypeId, string RequiredNote, int ToDistrictId, string ToWardCode,
    string FromName, string FromPhone, string FromAddress, int FromDistrictId, string FromWardCode,
    string FromProvinceName, string FromDistrictName, string FromWardName,
    string ToProvinceName, string ToDistrictName, string ToWardName,
    int ServiceTypeId = 2, int Length = 40, int Width = 40, int Height = 30);

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

public record IssueProcessingOperationDto(string? Notes);

public record CompleteProcessingOperationDto(string CompletionNotes);
public record ProcessingCatalogDto(List<ProcessingWarehouseDto> Warehouses,
    List<ProcessingOrganizationDto> Organizations, List<ProcessingCatalogItemDto> Items);
public record ProcessingWarehouseDto(Guid Id, string WarehouseName);
public record ProcessingOrganizationDto(Guid Id, string FullName, string OperationType);
public record ProcessingCatalogItemDto(Guid InventoryId, string Sku, string? BatchCode,
    string OperationType, string Grade, int Quantity, decimal Weight, string? LocationCode,
    bool IsLocked, string? LockReason);
