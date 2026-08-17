using DAL.Models.Commons;

namespace DAL.Models;

public class ClassifiedBatch : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public Guid? FabricTypeId { get; set; }
    public Guid? GarmentGroupId { get; set; }
    public Guid? ClothingTypeId { get; set; }
    public Guid? GenderId { get; set; }
    public Guid? TargetUserId { get; set; }
    public Guid? SizeId { get; set; }
    public Guid? ConditionGradeId { get; set; }
    public int ConditionRating { get; set; }
    public DateTime ClassificationDate { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public string FabricType { get; set; } = string.Empty;
    public string GarmentGroup { get; set; } = string.Empty;
    public string ClothingType { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string TargetUser { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string ProcessingDirection { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; }
    public int TotalItem { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ClassificationAreaName { get; set; }
    public DateTime? PlacedInClassificationAreaAt { get; set; }
    public Guid? PlacedInClassificationAreaByStaffId { get; set; }
    public DateTime? RemovedFromClassificationAreaAt { get; set; }
    public Guid? RemovedFromClassificationAreaByStaffId { get; set; }
    public DateTime? SentToWarehouseAt { get; set; }
    public Guid? SentToWarehouseByStaffId { get; set; }
    public DateTime? WarehouseReceivedAt { get; set; }
    public Guid? WarehouseReceivedByStaffId { get; set; }
    public DateTime? StoredAt { get; set; }
    public Guid? StoredByStaffId { get; set; }
    public decimal? ReceivedWeight { get; set; }
    public int? ReceivedItemCount { get; set; }
    public string? WarehouseReceiptNotes { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual AreaGroup? Group { get; set; }
    public virtual WarehouseArea? Area { get; set; }
    public virtual StorageLocation? StorageLocation { get; set; }
    public virtual User? SentToWarehouseByStaff { get; set; }
    public virtual User? PlacedInClassificationAreaByStaff { get; set; }
    public virtual User? RemovedFromClassificationAreaByStaff { get; set; }
    public virtual User? WarehouseReceivedByStaff { get; set; }
    public virtual User? StoredByStaff { get; set; }
    public virtual ICollection<ClassifiedItem> Items { get; set; } = new List<ClassifiedItem>();
    public virtual ICollection<ClassifiedBatchDonationRequest> DonationRequestSources { get; set; }
        = new List<ClassifiedBatchDonationRequest>();
}
