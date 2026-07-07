using DAL.Models.Commons;

namespace DAL.Models
{
    public class IntakeBatch : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public DateTime IntakeDate { get; set; }
        public decimal TotalWeight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        // Navigation
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual ICollection<DonationRequest> DonationRequests { get; set; }
            = new List<DonationRequest>();
    }
}
