using DAL.Models.Commons;

namespace DAL.Models
{
    public class PickupAssignment : BaseEntity
    {
        public DonationRequest DonorRequest { get; set; }
        public Guid DonorRequestId { get; set; }
        public Guid ReceivingStaffId { get; set; }
        public User ReceivingStaff { get; set; }
    }
}
