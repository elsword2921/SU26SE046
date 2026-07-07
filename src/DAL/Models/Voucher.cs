using DAL.Models.Commons;

namespace DAL.Models
{
    public class Voucher : BaseEntity
    {
        public string VoucherUrl { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
