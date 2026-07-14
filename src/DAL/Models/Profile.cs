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
        public virtual ICollection<ProfileDetail> Details { get; set; } = new List<ProfileDetail>();
        public virtual ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();
        public virtual ICollection<ClassifiedItem> ClassifiedItems { get; set; } = new List<ClassifiedItem>();
        public virtual ICollection<ClassifiedBatch> ClassifiedBatches { get; set; } = new List<ClassifiedBatch>();
    }
}
