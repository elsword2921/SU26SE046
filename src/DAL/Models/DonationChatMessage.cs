using DAL.Models.Commons;

namespace DAL.Models;

public class DonationChatMessage : BaseEntity
{
    public Guid DonationRequestId { get; set; }
    public Guid SenderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public virtual DonationRequest DonationRequest { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}
