using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class VoucherCode : BaseEntity
    {
        public Guid VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime ExpireDate { get; set; }
        public VoucherCodeStatus Status { get; set; }
            = VoucherCodeStatus.Available;
        public Guid? RedeemedByUserId { get; set; }
        public DateTime? RedeemedAt { get; set; }
        // Navigation
        public virtual Voucher Voucher { get; set; } = null!;
        public virtual User? RedeemedByUser { get; set; }
        public virtual VoucherRedemption? Redemption { get; set; }
    }
}