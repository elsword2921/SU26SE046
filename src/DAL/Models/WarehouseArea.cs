using DAL.Models.Commons;

namespace DAL.Models;

public class WarehouseArea : BaseEntity
{
    public byte[] RowVersion { get; set; } = [];
    public Guid WarehouseId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string AreaType { get; set; } = "Storage";
    public string? ProcessingDirection { get; set; }
    public string? Description { get; set; }
    public decimal CapacityKg { get; set; }
    public decimal CurrentKg { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual ICollection<AreaGroup> Groups { get; set; } = new List<AreaGroup>();
    public virtual ICollection<IntakeBatch> IntakeBatches { get; set; } = new List<IntakeBatch>();
}
