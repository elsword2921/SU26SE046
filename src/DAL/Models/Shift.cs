using DAL.Models.Commons;

namespace DAL.Models;

public class Shift : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ShiftDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual ICollection<OperationalTeam> Teams { get; set; } = new List<OperationalTeam>();
    public virtual ICollection<PickupAssignment> PickupAssignments { get; set; } = new List<PickupAssignment>();
    public virtual IntakeBatch? IntakeBatch { get; set; }
}
