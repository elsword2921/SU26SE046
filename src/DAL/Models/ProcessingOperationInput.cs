using DAL.Models.Commons;

namespace DAL.Models;

public class ProcessingOperationInput : BaseEntity
{
    public Guid ProcessingOperationId { get; set; }
    public Guid InventoryId { get; set; }
    public Guid? ClassifiedBatchId { get; set; }
    public int RequestedQuantity { get; set; }
    public decimal RequestedWeight { get; set; }
    public int IssuedQuantity { get; set; }
    public decimal IssuedWeight { get; set; }
    public string? Notes { get; set; }
    public virtual ProcessingOperation ProcessingOperation { get; set; }
        = null!;
    public virtual Inventory Inventory { get; set; }
        = null!;
    public virtual ClassifiedBatch? ClassifiedBatch { get; set; }
}