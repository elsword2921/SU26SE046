using DAL.Models.Commons;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DAL.Repository
{
    public class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected DbSet<T> _dbSet;

        public BaseRepository(AppDbContext context)
        {
            _dbSet = context.Set<T>();
        }
        public async Task<T> AddAsync(T entity)
        {
            _dbSet.Add(entity);
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<T> values)
        {
            foreach (var item in values)
            {
                _dbSet.Add(item);
            }
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.CountAsync(expression);
        }

        public async Task DeleteAsync(T entity, bool isHardDelete = false)
        {
            if (isHardDelete)
            {
                _dbSet.Remove(entity);
            }
            else
            {
                entity.IsActive = false;
                _dbSet.Update(entity);
            }
        }

        public async Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>>? expression = null, bool noTracked = false)
        {
            IQueryable<T> query;
            if (noTracked)
            {
                query = _dbSet.AsNoTracking();
            }
            else
            {
                query = _dbSet;
            }

            if (expression is not null)
            {
                query = _dbSet.Where(expression);
            }

            return query;
        }

        public async Task<T> GetByIdAsync(Guid Id, bool noTracked = false)
        {
            IQueryable<T> query;
            if (noTracked is true)
            {
                query = _dbSet.AsNoTracking();
            }
            else
            {
                query = _dbSet;
            }

            var entity = await query.FirstOrDefaultAsync(t => t.Id.Equals(Id));
            return entity;
        }

        public async Task<T> GetWithConditionAsync(Expression<Func<T, bool>> expression, bool noTracked = false, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (noTracked)
            {
                query = query.AsNoTracking();
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(expression);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }
    }
}
