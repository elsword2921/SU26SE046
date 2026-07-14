using DAL.Models.Commons;

namespace DAL.Models;

public class TransactionItem : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Guid InventoryId { get; set; }
    public Guid? ClassifiedBatchId { get; set; }
    public int Quantity { get; set; }
    public decimal Weight { get; set; }
    public string? Notes { get; set; }
    public virtual InventoryTransaction Transaction { get; set; } = null!;
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual ClassifiedBatch? ClassifiedBatch { get; set; }
}
