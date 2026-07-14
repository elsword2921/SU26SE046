using DAL.Models.Commons;

namespace DAL.Models;

public class IntakeBatchDonationRequest : BaseEntity
{
    public Guid IntakeBatchId { get; set; }
    public Guid DonationRequestId { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid AddedByStaffId { get; set; }
    public virtual IntakeBatch IntakeBatch { get; set; } = null!;
    public virtual DonationRequest DonationRequest { get; set; } = null!;
    public virtual User AddedByStaff { get; set; } = null!;
}
