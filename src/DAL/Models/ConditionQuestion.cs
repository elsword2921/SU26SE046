using DAL.Models.Commons;

namespace DAL.Models;

public class ConditionQuestion : BaseEntity
{
    public string QuestionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal Weight { get; set; }
    public bool IsCritical { get; set; }
    public virtual ICollection<ConditionAnswer> Answers { get; set; } = new List<ConditionAnswer>();
    public virtual ICollection<InspectionAnswer> InspectionAnswers { get; set; } = new List<InspectionAnswer>();
}
