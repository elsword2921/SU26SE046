using DAL.Models.Commons;

namespace DAL.Models
{
    public class DistributionRequest : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid WarehouseId { get; set; }
        public string ToAddress { get; set; } = string.Empty;
        public string? TrackingCode { get; set; }
        public string? CarrierName { get; set; }
        public string? ShippingPaymentType { get; set; }
        public decimal ShippingFee { get; set; }
        public DateTime? ActualDeliveryTime { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RequestNotes { get; set; }
        public string? RejectReason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual ICollection<DistributionItem> Items { get; set; } = new List<DistributionItem>();
    }
}
