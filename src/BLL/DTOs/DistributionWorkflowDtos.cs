namespace BLL.DTOs;
public record DistributionCatalogItemDto(Guid InventoryId, Guid ClassifiedBatchId, string BatchCode,
    string Sku, string ClothingType, string FabricType, string Gender, string TargetUser, string Size,
    string Grade, int AvailableQuantity, decimal AvailableWeight, List<DistributionCatalogImageDto> Items);
public record DistributionCatalogImageDto(string ItemCode, string ClothingType, string FabricType,
    string Gender, string TargetUser, string Size, List<string> ImageUrls, string? Notes);
public record CreateDistributionItemDto(Guid InventoryId, int Quantity);
public record CreateDistributionRequestDto(Guid WarehouseId, string RecipientName, string RecipientPhone,
    string ToAddress, string? Notes, List<CreateDistributionItemDto> Items);
public record CreateManagerRequestDto(
    Guid OrganizationId,
    Guid WarehouseId,
    string? Notes,
    List<CreateDistributionItemDto> Items
);
public record ApproveDistributionDto(bool Approved, string? Notes);
public record RespondDistributionRequestDto(bool Accepted, string? Notes);
public record IssueDistributionDto(string? Notes);
public record CreateGhnShipmentDto(int PaymentTypeId, string? RequiredNote, int ToDistrictId,
    string ToWardCode, int ServiceTypeId = 2);
public record DistributionItemViewDto(Guid Id, Guid InventoryId, string BatchCode, string Sku,
    string ClothingType, string FabricType, string Gender, string TargetUser, string Size,
    int RequestedQuantity, int ApprovedQuantity, int IssuedQuantity, decimal RequestedWeight, decimal IssuedWeight);
public record DistributionRequestViewDto(Guid Id, string Code, Guid OrganizationId, string OrganizationName,
    Guid WarehouseId, string WarehouseName, string RecipientName, string RecipientPhone, string ToAddress,
    string Status, string? Notes, string? RejectReason, DateTime RequestedAt, DateTime? ApprovedAt,
    string? IssueSlipCode, DateTime? WarehouseIssuedAt, string? IssuedBy, string? GhnOrderCode,
    string? GhnStatus, DateTime? GhnUpdatedAt, List<DistributionItemViewDto> Items,
    List<ShipmentEventDto> ShipmentHistory);
public record ShipmentEventDto(string Status, string? Description, string Source, DateTime OccurredAt);
