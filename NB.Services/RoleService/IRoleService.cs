using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.RoleService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.RoleService
{
    public interface IRoleService : IService<Role>
    {
       Task<RoleDto?> GetByRoleName(string name);
    }
}
