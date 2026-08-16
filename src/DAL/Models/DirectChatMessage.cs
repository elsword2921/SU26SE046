using DAL.Models.Commons;

namespace DAL.Models;

public class DirectChatMessage : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public virtual User Sender { get; set; } = null!;
    public virtual User Recipient { get; set; } = null!;
}
