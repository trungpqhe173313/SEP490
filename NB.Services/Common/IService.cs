using System.Linq.Expressions;

namespace NB.Service.Common
{
    public interface IService<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid? id);
        Task<T?> GetByIdAsync(int? id);
        IEnumerable<T> GetAll();
        Task<T> GetByIdOrThrowAsync(Guid? guid);
        Task CreateAsync(T entity);
        Task CreateAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        //Duc Ah
        Task UpdateNoTracking(T entity);
        Task UpdateAsync(IEnumerable<T> entities);
        Task DeleteAsync(T entity);
        IQueryable<T> GetQueryable();
        Task DeleteAsync(IEnumerable<T> entities);
        IEnumerable<T> FindBy(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> Where(Expression<Func<T, bool>> predicate);
        void Delete(Expression<Func<T, bool>> filter);
        Task DeleteRange(IEnumerable<T> entities);

        //Task<List<DropdownOption>> GetDropdownOptions(string displayField, string valueField, object? selected = null);
    }
}
