using DAL.Models;

namespace DAL.Repository
{
    public class DonorRequestRepository : BaseRepository<DonationRequest>, IDonorRequestRepository
    {
        public DonorRequestRepository(AppDbContext context) : base(context)
        {
        }
    }
}
