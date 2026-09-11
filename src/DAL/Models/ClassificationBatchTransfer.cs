using DAL.Models.Commons;

namespace DAL.Models;

public class ClassificationBatchTransfer : BaseEntity
{
    public Guid IntakeBatchId { get; set; }
    public Guid FromTeamId { get; set; }
    public Guid ToTeamId { get; set; }
    public Guid StaffId { get; set; }
    public DateTime TransferredAt { get; set; }
}
