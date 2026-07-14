using DAL.Models.Commons;

namespace DAL.Models;

public class InspectionAnswer : BaseEntity
{
    public Guid ClassifiedItemId { get; set; }
    public Guid ConditionQuestionId { get; set; }
    public Guid ConditionAnswerId { get; set; }
    public virtual ClassifiedItem ClassifiedItem { get; set; } = null!;
    public virtual ConditionQuestion ConditionQuestion { get; set; } = null!;
    public virtual ConditionAnswer ConditionAnswer { get; set; } = null!;
}
