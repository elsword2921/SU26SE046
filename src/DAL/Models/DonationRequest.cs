using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class DonationRequest : BaseEntity
    {
        public string RequestCode { get; set; } = string.Empty;

        public Guid DonorId { get; set; }

        public Guid WarehouseId { get; set; }

        public string ContactName { get; set; } = string.Empty;

        public string ContactPhoneNumber { get; set; } = string.Empty;

        public string DeliveryMethod { get; set; } = "StaffPickup";

        public string? DropOffMethod { get; set; }

        public string? CarrierName { get; set; }

        public string? TrackingCode { get; set; }

        public List<string>? ImageUrls { get; set; }

        public string? Description { get; set; }

        public decimal EstimateWeight { get; set; }

        public decimal? ActualWeight { get; set; }

        public string PickupAddress { get; set; } = string.Empty;

        public DateTime? PickupDate { get; set; }

        public string? RejectReason { get; set; }

        public DonationRequestStatus Status { get; set; }

        // Navigation

        public virtual User Donor { get; set; } = null!;

        public virtual Warehouse Warehouse { get; set; } = null!;

        public virtual ICollection<IntakeBatchDonationRequest> IntakeBatchDonationRequests { get; set; }
            = new List<IntakeBatchDonationRequest>();

        public virtual ICollection<PickupAssignment> PickupAssignments { get; set; }
            = new List<PickupAssignment>();

        public virtual ICollection<ClassifiedBatchDonationRequest> ClassifiedBatchDonationRequests { get; set; }
            = new List<ClassifiedBatchDonationRequest>();
        public virtual ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();
    }
}
