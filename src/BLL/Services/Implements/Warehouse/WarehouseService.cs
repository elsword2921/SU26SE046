using BLL.DTOs;
using BLL.Services.Interfaces.WarehouseService;
using DAL.Repository;

namespace BLL.Services.Implements.WarehouseService
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<WarehouseDto>> GetAllAsync()
        {
            var warehouses =
                await _unitOfWork
                .WarehouseRepository
                .GetAllAsync();

            return warehouses
                .Select(x => new WarehouseDto
                {
                    Id = x.Id,
                    Address = x.Address
                })
                .ToList();
        }
    }
}
