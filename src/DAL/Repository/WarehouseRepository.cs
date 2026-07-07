using DAL.Models;

namespace DAL.Repository
{
    public class WarehouseRepository : BaseRepository<Warehouse>, IWarehouseRepository
    {
        public WarehouseRepository(AppDbContext context) : base(context)
        {
        }
    }
}
