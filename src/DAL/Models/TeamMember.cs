using DAL.Models.Commons;

namespace DAL.Models;

public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }
    public Guid StaffId { get; set; }
    public virtual OperationalTeam Team { get; set; } = null!;
    public virtual User Staff { get; set; } = null!;
}
