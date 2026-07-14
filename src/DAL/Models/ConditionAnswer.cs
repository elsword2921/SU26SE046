using DAL.Models.Commons;

namespace DAL.Models;

public class ConditionAnswer : BaseEntity
{
    public Guid ConditionQuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int ConditionRating { get; set; }
    public virtual ConditionQuestion ConditionQuestion { get; set; } = null!;
    public virtual ICollection<InspectionAnswer> InspectionAnswers { get; set; } = new List<InspectionAnswer>();
}
