using NB.Model.Entities;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserRoleService
{
    public interface IUserRoleService : IService<UserRole>
    {
        Task<List<UserRole>?> GetByRoleId(int id);
    }
}
