using System.Linq.Expressions;

namespace DAL.Repository
{
    public interface IRepository<T> where T : class
    {
        Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>>? expression = null, bool noTracked = false);
        Task<T> GetWithConditionAsync(Expression<Func<T, bool>> expression, bool noTracked = false, params Expression<Func<T, object>>[] includes);
        Task<T> GetByIdAsync(Guid Id, bool noTracked = false);
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> values);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity, bool isHardDelete = false);
        Task<int> CountAsync(Expression<Func<T, bool>> expression);
    }
}