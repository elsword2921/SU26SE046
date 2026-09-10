using DAL.Models.Commons;

namespace DAL.Models;

public class ProcessingOperationOutput : BaseEntity
{
    public Guid ProcessingOperationId { get; set; }
    public string OutputType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Weight { get; set; }
    public int ReturnedQuantity { get; set; }
    public decimal ReturnedWeight { get; set; }
    public Guid? RecordedByStaffId { get; set; }
    public DateTime? RecordedAt { get; set; }
    public string? Notes { get; set; }
    public virtual ProcessingOperation ProcessingOperation { get; set; }
        = null!;
    public virtual User? RecordedByStaff { get; set; }
    public virtual ICollection<ClassifiedBatch> ClassifiedBatches { get; set; }
        = new List<ClassifiedBatch>();
}