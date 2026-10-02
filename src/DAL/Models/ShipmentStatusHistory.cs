using DAL.Models.Commons;

namespace DAL.Models;

public class ShipmentStatusHistory : BaseEntity
{
    public Guid DistributionRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Source { get; set; } = "System";
    public DateTime OccurredAt { get; set; }
    public virtual DistributionRequest DistributionRequest { get; set; } = null!;
}
