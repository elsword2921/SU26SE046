using DAL.Models.Commons;

namespace DAL.Models
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? AvatarUrl { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string UserStatus { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public int DonationPoint { get; set; } = 0;
        // Navigation
        public virtual Role Role { get; set; } = null!;
        public virtual Warehouse? Warehouse { get; set; }
        public virtual ICollection<DonationRequest> DonationRequests { get; set; }
            = new List<DonationRequest>();
        public virtual ICollection<UserVerificationCode> VerificationCodes { get; set; }
            = new List<UserVerificationCode>();
        public virtual ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();
        public virtual ICollection<VoucherCode> RedeemedVoucherCodes { get; set; }
            = new List<VoucherCode>();
        public virtual ICollection<VoucherRedemption> VoucherRedemptions { get; set; }
            = new List<VoucherRedemption>();
    }
}
