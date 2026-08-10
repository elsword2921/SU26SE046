using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class Voucher : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? VoucherUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? TermsAndConditions { get; set; }
        public decimal Value { get; set; }
        public int RequiredPoints { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public VoucherStatus Status { get; set; }
            = VoucherStatus.Active;
        // Navigation
        public virtual ICollection<VoucherCode> VoucherCodes { get; set; }
            = new List<VoucherCode>();
        public virtual ICollection<VoucherRedemption> VoucherRedemptions { get; set; }
            = new List<VoucherRedemption>();
    }
}
