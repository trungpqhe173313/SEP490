using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;

namespace NB.Service.UserRoleService
{
    public class UserRoleService : Service<UserRole>, IUserRoleService
    {
        public UserRoleService(IRepository<UserRole> repository) : base(repository)
        {
        }

        public async Task<List<UserRole>?> GetByRoleId(int id)
        {
            var query = from ur in GetQueryable()
                        where ur.RoleId == id
                        select new UserRole()
                        {
                            UserRoleId = ur.UserRoleId,
                            UserId = ur.UserId,
                            RoleId = ur.RoleId,
                            AssignedDate = ur.AssignedDate
                        };
            return await query.ToListAsync();
        }
    }
}
