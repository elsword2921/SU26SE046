using DAL.Models.Commons;

namespace DAL.Models
{
    public class Cart : BaseEntity
    {
        public Guid UserId { get; set; }
        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();
    }
}
