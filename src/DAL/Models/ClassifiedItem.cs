using DAL.Models.Commons;

namespace DAL.Models;

public class ClassifiedItem : BaseEntity
{
    public Guid BatchId { get; set; }
    public Guid? ClassifiedBatchId { get; set; }
    public Guid ProfileId { get; set; }
    public int ConditionRating { get; set; }
    public virtual IntakeBatch Batch { get; set; } = null!;
    public virtual ClassifiedBatch? ClassifiedBatch { get; set; }
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<InspectionAnswer> InspectionAnswers { get; set; } = new List<InspectionAnswer>();
}
