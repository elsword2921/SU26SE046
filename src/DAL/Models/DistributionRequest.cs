using DAL.Models.Commons;

namespace DAL.Models
{
    public class DistributionRequest : BaseEntity
    {
        public string RequestCode { get; set; } = string.Empty;
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
        public Guid? ApprovedByManagerId { get; set; }
        public DateTime? WarehouseIssuedAt { get; set; }
        public Guid? WarehouseIssuedByStaffId { get; set; }
        public string? IssueSlipCode { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string? GhnOrderCode { get; set; }
        public string? GhnStatus { get; set; }
        public DateTime? GhnUpdatedAt { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual User? ApprovedByManager { get; set; }
        public virtual User? WarehouseIssuedByStaff { get; set; }
        public virtual ICollection<DistributionItem> Items { get; set; } = new List<DistributionItem>();
        public virtual ICollection<ShipmentStatusHistory> ShipmentHistory { get; set; } = new List<ShipmentStatusHistory>();
    }
}
