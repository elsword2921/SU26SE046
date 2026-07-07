using DAL.Models.Commons;

namespace DAL.Models
{
    public class Warehouse : BaseEntity
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }

        // Navigation
        public virtual ICollection<User> Users { get; set; }
            = new List<User>();
        public virtual ICollection<DonationRequest> DonationRequests { get; set; }
            = new List<DonationRequest>();
        public virtual ICollection<IntakeBatch> IntakeBatches { get; set; }
            = new List<IntakeBatch>();
    }
}
