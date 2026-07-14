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
        public decimal TotalCapacityKg { get; set; }
        public decimal CurrentWeight { get; set; }

        // Navigation
        public virtual ICollection<User> Users { get; set; }
            = new List<User>();
        public virtual ICollection<DonationRequest> DonationRequests { get; set; }
            = new List<DonationRequest>();
        public virtual ICollection<IntakeBatch> IntakeBatches { get; set; }
            = new List<IntakeBatch>();
        public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
        public virtual ICollection<WarehouseArea> Areas { get; set; } = new List<WarehouseArea>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
