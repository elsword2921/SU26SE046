using DAL.Models.Commons;

namespace DAL.Models;

public class AiUsageLog : BaseEntity
{
    public Guid UserId { get; set; }
    public string Feature { get; set; } = string.Empty;
    public DateTime UsageDate { get; set; }
    public virtual User User { get; set; } = null!;
}
