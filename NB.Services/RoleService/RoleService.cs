using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.RoleService.Dto;

namespace NB.Service.RoleService
{
    public class RoleService : Service<Role>, IRoleService
    {
        public RoleService(IRepository<Role> repository) : base(repository)
        {
        }

        public async Task<RoleDto?> GetByRoleName(string name)
        {
            var query = from r in GetQueryable()
                        where r.RoleName.Contains(name)
                        select new RoleDto
                        {
                            RoleId = r.RoleId,
                            RoleName = r.RoleName,
                            Description = r.Description,
                            CreatedAt = r.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<RoleDto>> GetAllRoles()
        {
            var query = from r in GetQueryable()
                        select new RoleDto
                        {
                            RoleId = r.RoleId,
                            RoleName = r.RoleName,
                            Description = r.Description,
                            CreatedAt = r.CreatedAt
                        };
            return await query.ToListAsync();
        }
    }
}
