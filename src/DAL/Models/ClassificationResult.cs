using DAL.Models.Commons;

namespace DAL.Models;

public class ClassificationResult : BaseEntity
{
    public Guid CriteriaId { get; set; }
    public Guid OptionId { get; set; }
    public virtual ClassificationCriteria Criteria { get; set; } = null!;
    public virtual ClassificationCriteriaOption Option { get; set; } = null!;
}
