using DAL.Models.Commons;

namespace DAL.Models
{
    public class Profile : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Gender { get; set; }
        public string? AgeGroup { get; set; }
        public string? Size { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();
    }
}
