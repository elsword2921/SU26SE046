using DAL.Models;

namespace DAL.Repository
{
    public class PickupAssignmentRepository : BaseRepository<PickupAssignment>, IPickupAssignmentRepository
    {
        public PickupAssignmentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
