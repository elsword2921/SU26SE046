using DAL.Models.Commons;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DAL.Models
{
    public class CartItem : BaseEntity
    {
        public Guid CartId { get; set; }

        public Guid ProfileId { get; set; }

        public int ConditionRating { get; set; }

        public int Quantity { get; set; }

        // Navigation

        public virtual Cart Cart { get; set; } = null!;

        public virtual Profile Profile { get; set; } = null!;
    }
}
