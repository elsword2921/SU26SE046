using DAL.Models.Commons;

namespace DAL.Models
{
    public class Role : BaseEntity
    {
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Navigation
        public virtual ICollection<User> Users { get; set; }
            = new List<User>();
    }
}
