using DAL.Models.Commons;

namespace BLL.Services.Interfaces.Common;

public interface ICrudService<TEntity> where TEntity : BaseEntity
{
    Task<List<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity?> UpdateAsync(Guid id, TEntity entity);
    Task<bool> DeleteAsync(Guid id);
}
