using DAL.Models.Commons;

namespace DAL.Models;

public class AreaGroup : BaseEntity
{
    public Guid AreaId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CapacityKg { get; set; }
    public decimal CurrentKg { get; set; }
    public virtual WarehouseArea Area { get; set; } = null!;
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
