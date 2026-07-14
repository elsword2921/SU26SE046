using DAL.Models.Commons;

namespace DAL.Models;

public class OperationalTeam : BaseEntity
{
    public Guid ShiftId { get; set; }
    public string TeamType { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public virtual Shift Shift { get; set; } = null!;
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public virtual ICollection<IntakeBatch> IntakeBatches { get; set; } = new List<IntakeBatch>();
}
