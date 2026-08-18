namespace BLL.DTOs;

public record ClassificationBatchSummaryDto(Guid Id, string BatchCode, string RouteName,
    DateTime IntakeDate, decimal TotalWeight, string Status, int DonationRequests, int ClassifiedItems,
    int? CountedItemCount, decimal? CountedTotalWeight, DateTime? CountedAt,
    string? ClassificationAreaName, DateTime? ClassifiedAreaPlacedAt,
    Guid? ClassificationTeamId = null, string? ClassificationTeamName = null,
    string? TeamStatus = null, string? CurrentAreaName = null,
    DateTime? TeamShiftDate = null, TimeSpan? TeamShiftStartTime = null,
    TimeSpan? TeamShiftEndTime = null);

public record ClassificationItemDto(Guid Id, string ItemCode, string FabricType, string GarmentGroup,
    string ClothingType, string Gender, string TargetUser, string Size, string ConditionGrade,
    string ProcessingDirection, IReadOnlyList<string> ImageUrls, string? Notes, DateTime ClassifiedAt,
    Guid? FabricTypeId, Guid? GarmentGroupId, Guid? ClothingTypeId, Guid? GenderId,
    Guid? TargetUserId, Guid? SizeId, IReadOnlyList<ClassificationAnswerDto> Answers);

public record ClassificationBatchDetailDto(Guid Id, string BatchCode, string RouteName,
    DateTime IntakeDate, decimal TotalWeight, string Status, int DonationRequests,
    int? CountedItemCount, decimal? CountedTotalWeight, string? CountingNotes, DateTime? CountedAt,
    string? ClassificationAreaName, DateTime? ClassifiedAreaPlacedAt,
    IReadOnlyList<ClassificationItemDto> Items);

public class CountClassificationBatchDto
{
    public int ItemCount { get; set; }
    public decimal TotalWeightKg { get; set; }
    public string? Notes { get; set; }
}

public record ClassificationAnswerDto(Guid QuestionId, Guid AnswerId);

public class ClassifyItemDto
{
    public Guid FabricTypeId { get; set; }
    public Guid GarmentGroupId { get; set; }
    public Guid ClothingTypeId { get; set; }
    public Guid GenderId { get; set; }
    public Guid TargetUserId { get; set; }
    public Guid SizeId { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public string? Notes { get; set; }
    public List<ClassificationAnswerDto> Answers { get; set; } = [];
}

public record ClassificationOptionDto(Guid Id, string Text, string Grade);
public record ClassificationQuestionDto(Guid Id, string Text, int DisplayOrder,
    IReadOnlyList<ClassificationOptionDto> Options);
public record CategoryOptionDto(Guid Id, string Code, string Name, Guid? ParentId, int SortOrder);
public record ClassificationCatalogDto(IReadOnlyList<CategoryOptionDto> FabricTypes,
    IReadOnlyList<CategoryOptionDto> GarmentGroups, IReadOnlyList<CategoryOptionDto> ClothingTypes,
    IReadOnlyList<CategoryOptionDto> Genders, IReadOnlyList<CategoryOptionDto> TargetUsers,
    IReadOnlyList<CategoryOptionDto> Sizes, IReadOnlyList<CategoryOptionDto> ConditionGrades,
    IReadOnlyList<ClassificationQuestionDto> ConditionQuestions);

public class AnalyzeClassificationImagesDto
{
    public List<string> ImageDataUrls { get; set; } = [];
}

public record AiClassificationSuggestionDto(bool IsClothing, Guid? FabricTypeId, Guid? GarmentGroupId,
    Guid? ClothingTypeId, Guid? GenderId, Guid? TargetUserId, Guid? SizeId,
    IReadOnlyList<ClassificationAnswerDto> Answers, double Confidence, string Summary);

public record GroupedClassifiedBatchDto(Guid Id, string BatchCode, DateTime ClassificationDate,
    string FabricType, string GarmentGroup, string ClothingType, string Gender, string TargetUser,
    string Size, string ConditionGrade, string ProcessingDirection, int TotalItem, string Status,
    decimal TotalWeight, string? ClassificationAreaName, DateTime? PlacedInClassificationAreaAt,
    Guid? StorageLocationId, IReadOnlyList<string> DonationRequestCodes);

public record GroupedClassifiedBatchDetailDto(Guid Id, string BatchCode, DateTime ClassificationDate,
    string FabricType, string GarmentGroup, string ClothingType, string Gender, string TargetUser,
    string Size, string ConditionGrade, string ProcessingDirection, int TotalItem, string Status,
    decimal TotalWeight, string? ClassificationAreaName, DateTime? PlacedInClassificationAreaAt,
    IReadOnlyList<string> DonationRequestCodes, IReadOnlyList<ClassificationItemDto> Items);

public record SendGroupedBatchesToWarehouseDto(IReadOnlyList<Guid> GroupedBatchIds);
public record SendGroupedBatchesToWarehouseResultDto(int Sent, int Skipped);
public record PlaceGroupedClassifiedBatchDto(
    Guid AreaId,
    Guid GroupId,
    Guid StorageLocationId,
    decimal ActualWeightKg);

public record ClassificationAreaLayoutDto(Guid WarehouseId, string WarehouseName,
    IReadOnlyList<ClassificationAreaDto> Areas, IReadOnlyList<GroupedClassifiedBatchDto> UnassignedBatches);
public record ClassificationAreaDto(Guid Id, string AreaName, string? Description,
    decimal CapacityKg, decimal CurrentKg, IReadOnlyList<ClassificationAreaGroupDto> Groups);
public record ClassificationAreaGroupDto(Guid Id, string GroupName, string? Description,
    decimal CapacityKg, decimal CurrentKg, IReadOnlyList<ClassificationLocationDto> Locations,
    IReadOnlyList<GroupedClassifiedBatchDto> Batches);
public record ClassificationLocationDto(Guid Id, string LocationCode, string AisleCode,
    string RackCode, string ShelfCode, string BinCode, decimal CapacityKg,
    decimal CurrentWeightKg, string Status);

public record AssignClassificationBatchDto(Guid TeamId);
public record ClassificationStaffOptionDto(Guid Id, string FullName, string UserName,
    string PhoneNumber, Guid? WarehouseId);
public record ClassificationTeamDto(Guid Id, Guid ShiftId, string TeamName, string Status,
    DateTime ShiftDate, TimeSpan StartTime, TimeSpan EndTime, Guid WarehouseId, string WarehouseName,
    DateTime? StartedAt, DateTime? CompletedAt, IReadOnlyList<ReceivingTeamMemberDto> Members,
    int AssignedBatches, int CompletedBatches);
public record ClassificationManagementBatchDto(Guid Id, string BatchCode, string Status,
    Guid WarehouseId, string WarehouseName, decimal TotalWeight, int DonationRequests,
    Guid? TeamId, string? TeamName, string? CurrentAreaName, DateTime? SentAt);
public record ClassificationManagementBoardDto(
    IReadOnlyList<ManagerWarehouseOptionDto> Warehouses,
    IReadOnlyList<ClassificationStaffOptionDto> Staff,
    IReadOnlyList<ClassificationTeamDto> Teams,
    IReadOnlyList<ClassificationManagementBatchDto> Batches);
