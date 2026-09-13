namespace BLL.DTOs;
public record CharityRequestCriteriaDto(IReadOnlyList<CategoryOptionDto> ClothingTypes,
    IReadOnlyList<CategoryOptionDto> Genders, IReadOnlyList<CategoryOptionDto> Sizes,
    IReadOnlyList<CategoryOptionDto> TargetUsers);
public class CreateCharityDistributionRequestDto
{
    public Guid WarehouseId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? RequestedClothingTypeId { get; set; }
    public Guid? RequestedGenderId { get; set; }
    public Guid? RequestedSizeId { get; set; }
    public Guid? RequestedTargetUserId { get; set; }
    public decimal RequestedWeightKg { get; set; }
    public int? RequestedQuantity { get; set; }
}
public record CreateManagerRequestItemDto(Guid InventoryId, int Quantity);
public record CreateManagerRequestDto(
    Guid OrganizationId,
    Guid WarehouseId,
    string? Notes,
    List<CreateManagerRequestItemDto> Items
);
public record ApproveDistributionDto(bool Approved, string? Notes);
public record RespondDistributionRequestDto(bool Accepted, string? Notes);
public record IssueDistributionDto(string? Notes);
public record CreateGhnShipmentDto(int PaymentTypeId, string? RequiredNote, int ToDistrictId,
    string ToWardCode, string FromName, string FromPhone, string FromAddress,
    int FromDistrictId, string FromWardCode, int ServiceTypeId = 2);
public record DistributionItemViewDto(Guid Id, Guid InventoryId, string BatchCode, string Sku,
    string ClothingType, string FabricType, string Gender, string TargetUser, string Size,
    int RequestedQuantity, int ApprovedQuantity, int IssuedQuantity, decimal RequestedWeight, decimal IssuedWeight);
public record DistributionRequestViewDto(Guid Id, string Code, Guid OrganizationId, string OrganizationName,
    Guid WarehouseId, string WarehouseName, string WarehouseAddress, string? WarehousePhone,
    string RecipientName, string RecipientPhone, string ToAddress,
    string Status, string? Notes, string? RejectReason, DateTime RequestedAt, DateTime? ApprovedAt,
    string? IssueSlipCode, DateTime? WarehouseIssuedAt, string? IssuedBy, string? GhnOrderCode,
    string? GhnStatus, DateTime? GhnUpdatedAt, List<DistributionItemViewDto> Items,
    List<ShipmentEventDto> ShipmentHistory);
public record ShipmentEventDto(string Status, string? Description, string Source, DateTime OccurredAt);
