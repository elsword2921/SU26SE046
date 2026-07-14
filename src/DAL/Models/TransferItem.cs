using DAL.Models.Commons;

namespace DAL.Models;

public class TransferItem : BaseEntity
{
    public Guid TransferId { get; set; }
    public Guid ToAreaId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? ClassifiedBatchId { get; set; }
    public Guid RequestStaffId { get; set; }
    public Guid? ApproveStaffId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public virtual TransferRequest Transfer { get; set; } = null!;
    public virtual WarehouseArea ToArea { get; set; } = null!;
    public virtual IntakeBatch? Batch { get; set; }
    public virtual ClassifiedBatch? ClassifiedBatch { get; set; }
    public virtual User RequestStaff { get; set; } = null!;
    public virtual User? ApproveStaff { get; set; }
}
