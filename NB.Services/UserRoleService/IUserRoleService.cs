using NB.Model.Entities;
using NB.Service.Common;

namespace NB.Service.UserRoleService
{
    public interface IUserRoleService : IService<UserRole>
    {
        Task<List<UserRole>?> GetByRoleId(int id);
    }
}
