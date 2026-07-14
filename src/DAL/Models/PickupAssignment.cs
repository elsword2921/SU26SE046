using DAL.Models.Commons;

namespace DAL.Models
{
    public class PickupAssignment : BaseEntity
    {
        public DonationRequest DonorRequest { get; set; } = null!;
        public Guid DonorRequestId { get; set; }
        public Guid ShiftId { get; set; }
        public Guid TeamId { get; set; }
        public Guid IntakeBatchId { get; set; }
        public int RouteOrder { get; set; }
        public string AreaKey { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime? ProcessedAt { get; set; }
        public string? Notes { get; set; }
        public virtual Shift Shift { get; set; } = null!;
        public virtual OperationalTeam Team { get; set; } = null!;
        public virtual IntakeBatch IntakeBatch { get; set; } = null!;
    }
}
