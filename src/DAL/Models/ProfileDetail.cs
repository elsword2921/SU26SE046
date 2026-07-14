using DAL.Models.Commons;

namespace DAL.Models;

public class ProfileDetail : BaseEntity
{
    public Guid ProfileId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeValue { get; set; } = string.Empty;
    public virtual Profile Profile { get; set; } = null!;
}
