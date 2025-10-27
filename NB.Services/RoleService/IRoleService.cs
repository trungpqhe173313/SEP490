﻿using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.RoleService.Dto;

namespace NB.Service.RoleService
{
    public interface IRoleService : IService<Role>
    {
       Task<RoleDto?> GetByRoleName(string name);
    }
}
