using DAL.Models.Commons;

namespace DAL.Models
{
    public class VoucherRedemption : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid VoucherId { get; set; }
        public Guid VoucherCodeId { get; set; }
        public int PointsSpent { get; set; }
        public DateTime RedeemedAt { get; set; }
        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual Voucher Voucher { get; set; } = null!;
        public virtual VoucherCode VoucherCode { get; set; } = null!;
    }
}