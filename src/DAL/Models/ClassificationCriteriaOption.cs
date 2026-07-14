using DAL.Models.Commons;

namespace DAL.Models;

public class ClassificationCriteriaOption : BaseEntity
{
    public Guid CriteriaId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public virtual ClassificationCriteria Criteria { get; set; } = null!;
    public virtual ICollection<ClassificationResult> Results { get; set; } = new List<ClassificationResult>();
}
