using DAL.Models.Commons;

namespace DAL.Models;

public class ProcessingOperation : BaseEntity
{
    public byte[] RowVersion { get; set; } = [];
    public string OperationCode { get; set; } = string.Empty;

    /// Recycling or Disposal
    public string OperationType { get; set; } = string.Empty;

    /// PendingOrganizationApproval, RejectedByOrganization,
    /// PendingManagerApproval, RejectedByManager, Approved,
    /// ReadyForGhn, GhnBooked, InTransit, Delivered, DeliveryException, ShipmentCancelled, Returned,
    /// Issued (legacy), OrganizationReceived, Processing, AwaitingReturn, Completed, Cancelled
    public string Status { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ApprovedByOrganizationId { get; set; }
    public Guid? ApprovedByManagerId { get; set; }
    public DateTime? OrganizationRespondedAt { get; set; }
    public DateTime? ManagerRespondedAt { get; set; }
    public Guid? IssuedByStaffId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? OrganizationReceivedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    public DateTime? OutputReturnedAt { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ReturnDispatchedAt { get; set; }
    public string? ReturnCarrierName { get; set; }
    public string? ReturnTrackingCode { get; set; }
    public string? ReturnNotes { get; set; }
    public string? TrackingCode { get; set; }
    public string? CarrierName { get; set; }
    public string? GhnOrderCode { get; set; }
    public string? GhnStatus { get; set; }
    public DateTime? GhnUpdatedAt { get; set; }
    public virtual ICollection<ProcessingShipmentEvent> ShipmentHistory { get; set; } = new List<ProcessingShipmentEvent>();
    public string? RequestNotes { get; set; }
    public string? OrganizationRejectionReason { get; set; }
    public string? ManagerRejectionReason { get; set; }
    public Guid? RejectedByManagerId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? CompletionNotes { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual User Organization { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
    public virtual User? ApprovedByOrganization { get; set; }
    public virtual User? ApprovedByManager { get; set; }
    public virtual User? RejectedByManager { get; set; }
    public virtual User? IssuedByStaff { get; set; }
    public virtual ICollection<ProcessingOperationInput> Inputs { get; set; }
        = new List<ProcessingOperationInput>();
    public virtual ICollection<ProcessingOperationOutput> Outputs { get; set; }
        = new List<ProcessingOperationOutput>();
}
