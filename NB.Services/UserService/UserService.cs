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
                            Email = u.Email,
                            Image = u.Image,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    query = query.Where(u => u.FullName.Contains(search.FullName));
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
                            Email = u.Email,
                            Image = u.Image,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };

            return await query.FirstOrDefaultAsync();
        }

        public Task<List<UserDto>?> GetAllUser(UserSearch search)
        {
            var query = from u in GetQueryable()
                        select new UserDto
                        {
                            UserId = u.UserId,
                            FullName = u.FullName,
                            Email = u.Email,
                            Image = u.Image,
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    query = query.Where(u => u.FullName.Contains(search.FullName));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive);
                }
            }
            query = query.OrderByDescending(u => u.CreatedAt);
            return query.ToListAsync();
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
                            CreatedAt = u.CreatedAt,
                            IsActive = u.IsActive
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> CheckPasswordAsync(UserDto user, string password)
        {
            if (user == null || string.IsNullOrEmpty(password))
                return await Task.FromResult(false);

            var entity = await _userRepository
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (entity == null) return await Task.FromResult(false);
            if (entity.Password != password) return await Task.FromResult(false);
            return await Task.FromResult(true);
        }
    }
}
