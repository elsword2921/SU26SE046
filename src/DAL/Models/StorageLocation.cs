using DAL.Models.Commons;

namespace DAL.Models;

public class StorageLocation : BaseEntity
{
    public byte[] RowVersion { get; set; } = [];
    public Guid WarehouseId { get; set; }
    public Guid AreaId { get; set; }
    public Guid? AreaGroupId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string AisleCode { get; set; } = string.Empty;
    public string RackCode { get; set; } = string.Empty;
    public string ShelfCode { get; set; } = string.Empty;
    public string BinCode { get; set; } = string.Empty;
    public string? PreferredGarmentGroup { get; set; }
    public string? PreferredProcessingDirection { get; set; }
    public decimal CapacityKg { get; set; }
    public decimal CurrentWeightKg { get; set; }
    public string Status { get; set; } = "Available";
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual WarehouseArea Area { get; set; } = null!;
    public virtual AreaGroup? AreaGroup { get; set; }
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
