using DAL.Models.Commons;

namespace DAL.Models;

public class DonationPointTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? DonationRequestId { get; set; }
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public decimal? WeightKg { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual DonationRequest? DonationRequest { get; set; }
}
