namespace BLL.DTOs;

public record WarehouseDashboardDto(int PendingReceipt, int AwaitingPutaway, int StoredBatches,
    int AvailableQuantity, int InventorySkuCount, decimal AvailableWeightKg, decimal CapacityUsedPercent,
    decimal CurrentWeightKg, decimal CapacityKg);

public record WarehouseLayoutDto(Guid WarehouseId, string WarehouseName, string Address,
    decimal CapacityKg, decimal CurrentWeightKg, IReadOnlyList<WarehouseAreaLayoutDto> Areas);
public record WarehouseAreaLayoutDto(Guid Id, string AreaName, string? Description,
    string AreaType, decimal CapacityKg, decimal CurrentWeightKg,
    IReadOnlyList<WarehouseGroupLayoutDto> Groups,
    IReadOnlyList<WarehouseLocationLayoutDto> Locations,
    IReadOnlyList<WarehouseStagingBatchDto> IntakeBatches);
public record WarehouseStagingBatchDto(Guid Id, string BatchCode, string Status,
    decimal TotalWeight, DateTime IntakeDate, int DonationRequests, string? TeamName,
    Guid? StorageLocationId, string? LocationCode, string? GroupName,
    DateTime? WarehouseReceivedAt, string? WarehouseReceivedBy);
public record WarehouseGroupLayoutDto(Guid Id, string GroupName, string? Description,
    decimal CapacityKg, decimal CurrentWeightKg);
public record WarehouseLocationLayoutDto(Guid Id, Guid? AreaGroupId, string LocationCode, string AisleCode,
    string RackCode, string ShelfCode, string BinCode, string? PreferredGarmentGroup,
    string? PreferredProcessingDirection, decimal CapacityKg, decimal CurrentWeightKg,
    string Status, int InventoryCount, int ItemQuantity);

public record WarehouseInboundBatchDto(Guid Id, string BatchCode, DateTime ClassificationDate,
    string FabricType, string GarmentGroup, string ClothingType, string Gender, string TargetUser,
    string Size, string ConditionGrade, string ProcessingDirection, int ExpectedItemCount,
    decimal ExpectedWeightKg, string Status, DateTime? SentAt, DateTime? ReceivedAt,
    decimal? ReceivedWeightKg, int? ReceivedItemCount, string? ReceiptNotes,
    IReadOnlyList<string> DonationRequestCodes, IReadOnlyList<ClassificationItemDto> Items);

public record ConfirmWarehouseReceiptDto(decimal ActualWeightKg, int ActualItemCount,
    bool SealIntact, string? DiscrepancyNotes);

public record StorageLocationDto(Guid Id, string LocationCode, string AreaName, string AisleCode,
    string RackCode, string ShelfCode, string BinCode, string? PreferredGarmentGroup,
    string? PreferredProcessingDirection, decimal CapacityKg, decimal CurrentWeightKg,
    decimal AvailableCapacityKg, string Status, int MatchScore);

public record PutawayBatchDto(Guid LocationId, string? Notes);
public record IssueInventoryDto(decimal WeightKg, string Reason,
    string? ReferenceType, Guid? ReferenceId, string? Notes);
public record MoveInventoryDto(Guid DestinationLocationId, string Reason, string? Notes);

public record WarehouseInventoryDto(Guid Id, string Sku, Guid ClassifiedBatchId, string BatchCode,
    string LocationCode, string AreaName, string FabricType, string GarmentGroup,
    string ClothingType, string Gender, string TargetUser, string Size, string ConditionGrade,
    string ProcessingDirection, int Quantity, int ReservedQuantity, int AvailableQuantity,
    decimal TotalWeightKg, decimal ReservedWeightKg, decimal AvailableWeightKg, string Status,
    DateTime? StoredAt, IReadOnlyList<string> DonationRequestCodes);

public record WarehouseTransactionItemDto(Guid Id, Guid InventoryId, string Sku,
    string? ClassifiedBatchCode, int Quantity, decimal WeightKg, int QuantityBefore,
    int QuantityAfter, decimal WeightBefore, decimal WeightAfter, string? SourceLocationCode,
    string? DestinationLocationCode, string? Notes, IReadOnlyList<string> DonationRequestCodes);

public record WarehouseTransactionDto(Guid Id, string TransactionCode, string TransactionType,
    string? ReferenceType, Guid? ReferenceId, string Status, string? Notes,
    DateTime PerformedAt, string PerformedBy, IReadOnlyList<WarehouseTransactionItemDto> Items);

public record WarehouseIntakeTraceDto(Guid Id, string BatchCode, DateTime IntakeDate,
    string Status, string? RouteName, int DonationRequests, int ClassifiedItems,
    IReadOnlyList<WarehouseClassifiedBatchTraceDto> ClassifiedBatches);
public record WarehouseClassifiedBatchTraceDto(Guid Id, string BatchCode, string Status,
    string ClothingType, string ConditionGrade, string ProcessingDirection,
    int ItemCount, decimal WeightKg, string? InventorySku, string? LocationCode,
    IReadOnlyList<string> DonationRequestCodes);

public record SaveWarehouseAreaDto(Guid WarehouseId, string AreaName, string? Description,
    decimal CapacityKg, string AreaType = "Storage");
public record CreateWarehouseDto(string WarehouseName, string Address, string? PhoneNumber,
    string? Email, string? Description, decimal TotalCapacityKg, double? Latitude, double? Longitude,
    double ServiceRadiusKm = 24);
public record WarehouseDetailsDto(Guid Id, string WarehouseName, string Address, string? PhoneNumber,
    string? Email, string? Description, decimal TotalCapacityKg, decimal CurrentWeightKg,
    decimal AllocatedAreaCapacityKg, double? Latitude, double? Longitude, double ServiceRadiusKm);
public record SaveWarehouseGroupDto(Guid AreaId, string GroupName, string? Description,
    decimal CapacityKg);
public record SaveStorageLocationDto(Guid AreaGroupId, string LocationCode, string AisleCode,
    string RackCode, string ShelfCode, string BinCode, string? PreferredGarmentGroup,
    string? PreferredProcessingDirection, decimal CapacityKg, string Status);
