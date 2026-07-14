using BLL.Services.Interfaces.Common;
using DAL;
using DAL.Models.Commons;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.Common;

public class CrudService<TEntity> : ICrudService<TEntity> where TEntity : BaseEntity
{
    private readonly AppDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public CrudService(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public Task<List<TEntity>> GetAllAsync()
    {
        return _dbSet
            .AsNoTracking()
            .Where(entity => entity.IsActive != false)
            .OrderByDescending(entity => entity.CreateAt)
            .ToListAsync();
    }

    public Task<TEntity?> GetByIdAsync(Guid id)
    {
        return _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id && entity.IsActive != false);
    }

    public async Task<TEntity> CreateAsync(TEntity entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreateAt = DateTime.UtcNow;
        entity.UpdateAt = null;
        entity.DeleteAt = null;
        entity.DeletedBy = null;
        entity.IsActive = true;

        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<TEntity?> UpdateAsync(Guid id, TEntity entity)
    {
        var current = await _dbSet
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive != false);

        if (current is null)
        {
            return null;
        }

        var createdAt = current.CreateAt;
        var createdBy = current.CreatedBy;

        _context.Entry(current).CurrentValues.SetValues(entity);
        current.Id = id;
        current.CreateAt = createdAt;
        current.CreatedBy = createdBy;
        current.UpdateAt = DateTime.UtcNow;
        current.DeleteAt = null;
        current.DeletedBy = null;
        current.IsActive = true;

        await _context.SaveChangesAsync();
        return current;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _dbSet
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive != false);

        if (entity is null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.DeleteAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
