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

        //public async Task<PagedList<UserDto>> GetData(UserSearch search)
        //{
        //    var query = from u in GetQueryable()
        //                join ur in _userRoleRepository.GetQueryable() on u.UserId equals ur.UserId into userRoles
        //                from ur in userRoles.DefaultIfEmpty()
        //                join r in _roleRepository.GetQueryable() on ur.RoleId equals r.RoleId into roles
        //                from r in roles.DefaultIfEmpty()
        //                group r by new
        //                {
        //                    u.UserId,
        //                    u.FullName,
        //                    u.Email,
        //                    u.Image,
        //                    u.CreatedAt
        //                } into g
        //                select new UserDto
        //                {
        //                    UserId = g.Key.UserId,
        //                    FullName = g.Key.FullName,
        //                    Email = g.Key.Email,
        //                    Image = g.Key.Image,
        //                    CreatedAt = g.Key.CreatedAt
        //                };
        //    if (search != null)
        //    {
        //        if (!string.IsNullOrEmpty(search.FullName))
        //        {
        //            query = query.Where(u => u.FullName.Contains(search.FullName));
        //        }
        //        if (search.IsActive.HasValue)
        //        {
        //            query = query.Where(u => u.IsActive == search.IsActive);
        //        }
        //    }
        //    query = query.OrderByDescending(u => u.CreatedAt);
        //    return await PagedList<UserDto>.CreateAsync(query, search);
        //}

        //public async Task<PagedList<UserDto>> GetData(UserSearch search)
        //{
        //    // 1️⃣ Lấy dữ liệu từ DB bằng EF Core — chỉ join và select các cột cần thiết
        //    var data = await (
        //        from u in GetQueryable()
        //        join ur in _userRoleRepository.GetQueryable() on u.UserId equals ur.UserId into userRoles
        //        from ur in userRoles.DefaultIfEmpty()
        //        join r in _roleRepository.GetQueryable() on ur.RoleId equals r.RoleId into roles
        //        from r in roles.DefaultIfEmpty()
        //        select new
        //        {
        //            u.UserId,
        //            u.FullName,
        //            u.Email,
        //            u.Image,
        //            u.CreatedAt,
        //            u.IsActive,
        //            RoleName = r.RoleName
        //        }
        //    ).ToListAsync(); // 👉 thực thi SQL trước (Entity Framework có thể dịch được tới đây)

        //    // 2️⃣ GroupBy trên bộ nhớ (LINQ thuần)
        //    var grouped = data
        //        .GroupBy(u => new
        //        {
        //            u.UserId,
        //            u.FullName,
        //            u.Email,
        //            u.Image,
        //            u.CreatedAt,
        //            u.IsActive
        //        })
        //        .Select(g => new UserDto
        //        {
        //            UserId = g.Key.UserId,
        //            FullName = g.Key.FullName,
        //            Email = g.Key.Email,
        //            Image = g.Key.Image,
        //            CreatedAt = g.Key.CreatedAt,
        //            IsActive = g.Key.IsActive,
        //            RoleNames = g.Where(x => x.RoleName != null).Select(x => x.RoleName!).Distinct().ToList()
        //        })
        //        .AsQueryable(); // 👉 để PagedList có thể tiếp tục xử lý (Skip/Take)

        //    // 3️⃣ Áp dụng điều kiện tìm kiếm (filter)
        //    if (search != null)
        //    {
        //        //if (!string.IsNullOrEmpty(search.FullName))
        //        //{
        //        //    var name = search.FullName.ToLower();
        //        //    grouped = grouped.Where(u => u.FullName.ToLower().Contains(name));
        //        //}

        //        //if (search.IsActive.HasValue)
        //        //{
        //        //    grouped = grouped.Where(u => u.IsActive == search.IsActive);
        //        //}

        //        if (!string.IsNullOrEmpty(search.RoleName))
        //        {
        //            var role = search.RoleName.ToLower();
        //            grouped = grouped.Where(u => u.RoleNames.Any(rn => rn.ToLower().Contains(role)));
        //        }
        //    }

        //    // 4️⃣ Sắp xếp và trả về kết quả phân trang
        //    //grouped = grouped.OrderByDescending(u => u.CreatedAt);

        //    return await PagedList<UserDto>.CreateAsync(grouped, search);
        //}

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
                        where u.Email == username
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
    }
}
