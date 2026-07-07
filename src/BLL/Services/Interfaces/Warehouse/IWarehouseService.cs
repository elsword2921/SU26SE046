using BLL.DTOs;

namespace BLL.Services.Interfaces.WarehouseService
{
    public interface IWarehouseService
    {
        Task<List<WarehouseDto>> GetAllAsync();
    }
}
