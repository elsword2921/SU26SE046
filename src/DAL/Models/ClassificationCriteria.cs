using DAL.Models.Commons;

namespace DAL.Models;

public class ClassificationCriteria : BaseEntity
{
    public string CriteriaName { get; set; } = string.Empty;
    public string? CriteriaDescription { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public virtual ICollection<ClassificationCriteriaOption> Options { get; set; } = new List<ClassificationCriteriaOption>();
    public virtual ICollection<ClassificationResult> Results { get; set; } = new List<ClassificationResult>();
}
