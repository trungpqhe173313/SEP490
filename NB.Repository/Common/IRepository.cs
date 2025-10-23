using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.Common
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid? id);
        Task<T?> GetByIdAsync(int? id);

        IEnumerable<T> GetAll();

        IQueryable<T> GetQueryable();

        IEnumerable<T> FindBy(Expression<Func<T, bool>> predicate);

        IQueryable<T> Where(Expression<Func<T, bool>> predicate);

        T Add(T entity);

        T Delete(T entity);

        void Update(T entity);

        Task SaveAsync();

        void DeleteRange(IEnumerable<T> entities);

        void CreateRange(IEnumerable<T> entities);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        void Delete(Expression<Func<T, bool>> filter);
    }
}
