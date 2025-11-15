using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Repository.RoleRepository;
using NB.Repository.UserRolerRepository;
using NB.Service.AccountService.Dto;
using NB.Service.AdminService.Dto;
using NB.Service.CategoryService;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AdminService
{
    public class AdminService : Service<User>, IAdminService
    {
        private readonly IUserRolerRepository _userRoleRepository;
        private readonly IRoleRepository _roleRepository;
        public AdminService(
            IRepository<User> repository,
            IUserRolerRepository userRoleRepository,
            IRoleRepository roleRepository) : base(repository)
        {
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
        }

        public async Task<PagedList<AccountDto>> GetData(AccountSearch search)
        {
            var query = GetQueryable().Select(u => new AccountDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                Image = u.Image,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                Roles = _userRoleRepository.GetQueryable()
                    .Where(r => r.UserId == u.UserId)
                    .Join(_roleRepository.GetQueryable(),
                            ur => ur.RoleId,
                            r => r.RoleId,
                            (ur, r) => r.RoleName)
                    .ToList()
            });

            // Search conditions
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    var keyword = search.FullName.Trim();
                    query = query.Where(u =>
                        EF.Functions.Collate(u.FullName, "SQL_Latin1_General_CP1_CI_AI")
                        .Contains(keyword));
                }

                if (!string.IsNullOrEmpty(search.Email))
                {
                    var keyword = search.Email.Trim();
                    query = query.Where(u =>
                        EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AI")
                        .Contains(keyword));
                }

                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive);
                }
            }

            query = query.OrderByDescending(u => u.CreatedAt);

            return await PagedList<AccountDto>.CreateAsync(query, search);
        }

        //public async Task<User?> GetByUserId(int id)
        //{
        //    return await GetQueryable()
        //        .Where(u => u.UserId == id)
        //        .FirstOrDefaultAsync();
        //}
    }
}
