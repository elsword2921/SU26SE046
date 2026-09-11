using DAL.Models.Commons;

namespace DAL.Models;

public class ProcessingShipmentEvent : BaseEntity
{
    public Guid ProcessingOperationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }
    public virtual ProcessingOperation ProcessingOperation { get; set; } = null!;
}
