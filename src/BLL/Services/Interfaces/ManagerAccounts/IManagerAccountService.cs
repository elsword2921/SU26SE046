using BLL.DTOs;

namespace BLL.Services.Interfaces.ManagerAccounts;

public interface IManagerAccountService
{
    Task<ManagerAccountPageDto> SearchAsync(Guid? warehouseId, string? role, string? search, int page, int pageSize);
    Task<Guid> CreateAsync(Guid managerId, CreateManagerAccountDto dto);
    Task UpdateAsync(Guid managerId, Guid userId, UpdateManagerAccountDto dto);
    Task SetLockedAsync(Guid managerId, Guid userId, bool locked);
    Task DeleteAsync(Guid managerId, Guid userId);
}
