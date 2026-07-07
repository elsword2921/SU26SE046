using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class DonationRequest : BaseEntity
    {
        public Guid DonorId { get; set; }

        public Guid WarehouseId { get; set; }

        public Guid? BatchId { get; set; }

        public List<string>? ImageUrls { get; set; }

        public string? Description { get; set; }

        public decimal EstimateWeight { get; set; }

        public decimal? ActualWeight { get; set; }

        public string PickupAddress { get; set; } = string.Empty;

        public DateTime? PickupDate { get; set; }

        public string? RejectReason { get; set; }

        public string Status { get; set; } = string.Empty;

        // Navigation

        public virtual User Donor { get; set; } = null!;

        public virtual Warehouse Warehouse { get; set; } = null!;

        public virtual IntakeBatch? Batch { get; set; }
    }
}
