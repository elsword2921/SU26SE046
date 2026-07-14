using DAL.Models.Commons;

namespace DAL.Models
{
    public class DistributionItem : BaseEntity
    {
        public Guid DistributionRequestId { get; set; }
        public Guid ProfileId { get; set; }
        public int ConditionRating { get; set; }
        public int RequestedQuantity { get; set; }
        public string? Notes { get; set; }
        public virtual DistributionRequest DistributionRequest { get; set; } = null!;
        public virtual Profile Profile { get; set; } = null!;
    }
}
