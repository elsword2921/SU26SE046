using DAL.Models.Commons;

namespace DAL.Models;

public class TransferRequest : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid FromAreaId { get; set; }
    public Guid ToAreaId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReceivedAt { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual WarehouseArea FromArea { get; set; } = null!;
    public virtual WarehouseArea ToArea { get; set; } = null!;
    public virtual ICollection<TransferItem> Items { get; set; } = new List<TransferItem>();
}
