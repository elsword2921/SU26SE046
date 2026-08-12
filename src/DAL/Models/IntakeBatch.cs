using DAL.Models.Commons;

namespace DAL.Models
{
    public class IntakeBatch : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public Guid ShiftId { get; set; }
        public Guid? ReceivingTeamId { get; set; }
        public Guid? ClassificationTeamId { get; set; }
        public Guid? CurrentAreaId { get; set; }
        public Guid? CurrentAreaGroupId { get; set; }
        public List<string>? BatchImages { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? SentToClassificationAt { get; set; }
        public DateTime? WarehouseReceivedAt { get; set; }
        public Guid? WarehouseReceivedByStaffId { get; set; }
        public DateTime? ClassificationAssignedAt { get; set; }
        public Guid? ClassificationAssignedByManagerId { get; set; }
        public DateTime? ClassificationReceivedAt { get; set; }
        public Guid? ClassificationReceivedByStaffId { get; set; }
        public int? CountedItemCount { get; set; }
        public decimal? CountedTotalWeight { get; set; }
        public string? CountingNotes { get; set; }
        public DateTime? CountedAt { get; set; }
        public Guid? CountedByStaffId { get; set; }
        public DateTime? ClassificationStartedAt { get; set; }
        public Guid? ClassificationStartedByStaffId { get; set; }
        public DateTime? ClassificationCompletedAt { get; set; }
        public Guid? ClassificationCompletedByStaffId { get; set; }
        public DateTime? ClassifiedAreaPlacedAt { get; set; }
        public Guid? ClassifiedAreaPlacedByStaffId { get; set; }
        public string? ClassificationAreaName { get; set; }
        public DateTime IntakeDate { get; set; }
        public decimal TotalWeight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        // Navigation
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual Shift Shift { get; set; } = null!;
        public virtual OperationalTeam? ReceivingTeam { get; set; }
        public virtual OperationalTeam? ClassificationTeam { get; set; }
        public virtual WarehouseArea? CurrentArea { get; set; }
        public virtual AreaGroup? CurrentAreaGroup { get; set; }
        public virtual User? WarehouseReceivedByStaff { get; set; }
        public virtual User? ClassificationAssignedByManager { get; set; }
        public virtual User? ClassificationReceivedByStaff { get; set; }
        public virtual User? CountedByStaff { get; set; }
        public virtual User? ClassificationStartedByStaff { get; set; }
        public virtual User? ClassificationCompletedByStaff { get; set; }
        public virtual User? ClassifiedAreaPlacedByStaff { get; set; }
        public virtual ICollection<IntakeBatchDonationRequest> IntakeBatchDonationRequests { get; set; }
            = new List<IntakeBatchDonationRequest>();
        public virtual ICollection<ClassifiedBatchDonationRequest> ClassifiedBatchSources { get; set; }
            = new List<ClassifiedBatchDonationRequest>();
        public virtual ICollection<ClassifiedItem> ClassifiedItems { get; set; }
            = new List<ClassifiedItem>();
        public virtual ICollection<PickupAssignment> PickupAssignments { get; set; }
            = new List<PickupAssignment>();
    }
}
