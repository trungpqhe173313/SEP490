using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService
{
    public class UserService : Service<User>, IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly IRepository<Role> _roleRepository;

        public UserService(IRepository<User> userRepository,
            IRepository<UserRole> userRoleRepository,
            IRepository<Role> roleRepository) : base(userRepository)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
        }

        public async Task<PagedList<UserDto>> GetData(UserSearch search)
        {
            var query = from u in GetQueryable()
                        select new UserDto
                        {
                            UserId = u.UserId,
                            FullName = u.FullName,
                            //Username = u.Username,
                            //Password = u.Password,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    var keyword = search.FullName.Trim();
                    query = query.Where(u => EF.Functions.Collate(u.FullName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (!string.IsNullOrEmpty(search.Email))
                {
                    var keyword = search.Email.Trim();
                    query = query.Where(u => EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive);
                }
            }
            query = query.OrderByDescending(u => u.CreatedAt);
            return await PagedList<UserDto>.CreateAsync(query, search);
        }

        public async Task<UserDto?> GetByUserId(int id)
        {
            var query = from u in GetQueryable()
                        where u.UserId == id
                        select new UserDto
                        {
                            UserId = u.UserId,
                            FullName = u.FullName,
                            Username = u.Username,
                            Password = u.Password,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive,
                            RefreshToken = u.RefreshToken,
                            RefreshTokenExpiryDate = u.RefreshTokenExpiryDate
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<UserDto>?> GetAllUser(UserSearch search)
        {
            var query = from u in GetQueryable()
                        select new UserDto
                        {
                            UserId = u.UserId,
                            FullName = u.FullName,
                            Username = u.Username,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    var keyword = search.FullName.Trim();
                    query = query.Where(u => EF.Functions.Collate(u.FullName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (!string.IsNullOrEmpty(search.Email))
                {
                    var keyword = search.Email.Trim();
                    query = query.Where(u => EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive);
                }
            }

            query = query.OrderByDescending(u => u.CreatedAt);
            return await query.ToListAsync();
        }

        public async Task<UserDto?> GetByEmail(string email)
        {
            var query = from u in GetQueryable()
                        where u.Email == email
                        select new UserDto
                        {
                            UserId = u.UserId,
                            FullName = u.FullName,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<UserDto?> GetByUsername(string username)
        {
            var query = from u in GetQueryable()
                        where u.Username == username
                        select new UserDto
                        {
                            UserId = u.UserId,
                            Username = u.Username,
                            FullName = u.FullName,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            if (user == null || string.IsNullOrEmpty(password))
                return await Task.FromResult(false);

            var entity = await 
                GetQueryable()
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (entity == null) return await Task.FromResult(false);

            // MIGRATION STRATEGY: Hỗ trợ cả plain text và hashed password
            if (PasswordHasher.IsBCryptHash(entity.Password))
            {
                // Password đã được hash → Verify bằng BCrypt
                var isValid = PasswordHasher.VerifyPassword(password, entity.Password);
                return await Task.FromResult(isValid);
            }
            else
            {
                // Password còn plain text → So sánh trực tiếp
                if (entity.Password != password)
                    return await Task.FromResult(false);

                // ✅ Password đúng → MIGRATE sang hash ngay
                try
                {
                    entity.Password = PasswordHasher.HashPassword(password);
                    _userRepository.Update(entity);
                    await _userRepository.SaveAsync();
                    
                    // Log migration
                    Console.WriteLine($"[PASSWORD MIGRATION] User {entity.Username} (ID: {entity.UserId}) migrated to hashed password");
                }
                catch (Exception ex)
                {
                    // Nếu migration fail, vẫn cho login (không block user)
                    Console.WriteLine($"[PASSWORD MIGRATION ERROR] User {entity.Username}: {ex.Message}");
                }

                return await Task.FromResult(true);
            }
        }

        public async Task<UserDto?> GetByRefreshTokenAsync(string RefreshToken)
        {
            var query = from u in GetQueryable()
                        where u.RefreshToken == RefreshToken && u.IsActive == true
                        select new UserDto
                        {
                            UserId = u.UserId,
                            Username = u.Username,
                            FullName = u.FullName,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive,
                            RefreshToken = u.RefreshToken,
                            RefreshTokenExpiryDate = u.RefreshTokenExpiryDate
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<UserDto>?> GetAllUserForAdmin(UserSearch search)
        {
            var query = from u in GetQueryable()
                        select new UserDto
                        {
                            UserId = u.UserId,
                            FullName = u.FullName,
                            Username = u.Username,
                            Password = u.Password,
                            Email = u.Email,
                            Image = u.Image,
                            Phone = u.Phone,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    var keyword = search.FullName.Trim();
                    query = query.Where(u => EF.Functions.Collate(u.FullName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (!string.IsNullOrEmpty(search.Email))
                {
                    var keyword = search.Email.Trim();
                    query = query.Where(u => EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive);
                }
            }

            query = query.OrderByDescending(u => u.CreatedAt);
            return await query.ToListAsync();
        }
    }
}
