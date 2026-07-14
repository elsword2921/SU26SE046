using DAL.Models.Commons;

namespace DAL.Models;

public class ClassifiedBatch : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProfileId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? AreaId { get; set; }
    public int ConditionRating { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; }
    public int TotalItem { get; set; }
    public string Status { get; set; } = string.Empty;
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Profile Profile { get; set; } = null!;
    public virtual AreaGroup? Group { get; set; }
    public virtual WarehouseArea? Area { get; set; }
    public virtual ICollection<ClassifiedItem> Items { get; set; } = new List<ClassifiedItem>();
}
