using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
