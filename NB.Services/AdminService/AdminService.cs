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

        public async Task<ApiResponse<bool>> UpdateAccountAsync(int id, UpdateAccountDto dto)
        {
            if (dto == null)
                return ApiResponse<bool>.Fail("Dữ liệu không hợp lệ", 400);

            // Lấy user
            var user = await GetQueryable().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
                return ApiResponse<bool>.Fail("Không tìm thấy tài khoản", 404);

            // Validate phone trùng
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var phoneExists = await GetQueryable()
                    .AnyAsync(u => u.Phone == dto.Phone && u.UserId != id);

                if (phoneExists)
                    return ApiResponse<bool>.Fail("Số điện thoại đã tồn tại", 409);
            }

            // Validate email trùng
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var emailExists = await GetQueryable()
                    .AnyAsync(u => u.Email == dto.Email && u.UserId != id);

                if (emailExists)
                    return ApiResponse<bool>.Fail("Email đã tồn tại", 409);
            }

            // Cập nhật dữ liệu cơ bản
            user.Username = dto.UserName ?? user.Username;
            user.Email = dto.Email ?? user.Email;
            user.FullName = dto.FullName ?? user.FullName;
            user.Phone = dto.Phone ?? user.Phone;
            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            // Cập nhật roles
            if (dto.Roles != null && dto.Roles.Count > 0)
            {
                // Xóa role cũ
                var oldRoles = _userRoleRepository
                    .GetQueryable()
                    .Where(ur => ur.UserId == id)
                    .ToList();

                foreach (var ur in oldRoles)
                    _userRoleRepository.Delete(ur);

                // Thêm role mới
                foreach (var roleName in dto.Roles)
                {
                    var role = _roleRepository
                        .GetQueryable()
                        .FirstOrDefault(r => r.RoleName == roleName);

                    if (role != null)
                    {
                        _userRoleRepository.Add(new UserRole
                        {
                            UserId = id,
                            RoleId = role.RoleId
                        });
                    }
                }
            }

            // Lưu thay đổi
            await _userRoleRepository.SaveAsync();
            await UpdateAsync(user);

            return ApiResponse<bool>.Ok(true);
        }
    }
}
