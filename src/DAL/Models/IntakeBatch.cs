using DAL.Models.Commons;

namespace DAL.Models
{
    public class IntakeBatch : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public Guid ShiftId { get; set; }
        public Guid? ReceivingTeamId { get; set; }
        public List<string>? BatchImages { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime IntakeDate { get; set; }
        public decimal TotalWeight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        // Navigation
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual Shift Shift { get; set; } = null!;
        public virtual OperationalTeam? ReceivingTeam { get; set; }
        public virtual ICollection<IntakeBatchDonationRequest> IntakeBatchDonationRequests { get; set; }
            = new List<IntakeBatchDonationRequest>();
        public virtual ICollection<ClassifiedItem> ClassifiedItems { get; set; }
            = new List<ClassifiedItem>();
        public virtual ICollection<PickupAssignment> PickupAssignments { get; set; }
            = new List<PickupAssignment>();
    }
}
