using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
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
        private readonly IRepository<Transaction> _transactionRepository;

        public UserService(IRepository<User> userRepository,
            IRepository<UserRole> userRoleRepository,
            IRepository<Role> roleRepository,
            IRepository<Transaction> transactionRepository) : base(userRepository)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
            _transactionRepository = transactionRepository;
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

        public async Task<List<TopCustomerDto>> GetTopCustomersByTotalSpent(DateTime fromDate, DateTime toDate)
        {
            // Tính toán ngày kết thúc (bao gồm cả ngày cuối cùng)
            var toDateEnd = toDate.Date.AddDays(1).AddSeconds(-1);

            // Bao gồm các đơn hàng đã hoàn thành hoặc đã thanh toán
            var validStatuses = new List<int>
            {
                (int)TransactionStatus.done,
                (int)TransactionStatus.paidInFull,
                (int)TransactionStatus.partiallyPaid
            };

            // Query để lấy top 10 khách hàng mua hàng nhiều nhất
            var topCustomersQuery = from transaction in _transactionRepository.GetQueryable()
                                   join user in GetQueryable()
                                       on transaction.CustomerId equals user.UserId
                                   where transaction.Type == "Export"
                                      && validStatuses.Contains((int)transaction.Status)
                                      && transaction.TransactionDate >= fromDate
                                      && transaction.TransactionDate <= toDateEnd
                                      && transaction.CustomerId.HasValue
                                      && transaction.TotalCost.HasValue
                                   group new { transaction, user } by new
                                   {
                                       user.UserId,
                                       user.FullName,
                                       user.Email,
                                       user.Phone,
                                       user.Image
                                   } into grouped
                                   select new TopCustomerDto
                                   {
                                       UserId = grouped.Key.UserId,
                                       FullName = grouped.Key.FullName ?? string.Empty,
                                       Email = grouped.Key.Email ?? string.Empty,
                                       Phone = grouped.Key.Phone ?? string.Empty,
                                       Image = grouped.Key.Image,
                                       TotalSpent = grouped.Sum(x => x.transaction.TotalCost ?? 0),
                                       NumberOfOrders = grouped.Count(),
                                       AverageOrderValue = grouped.Average(x => x.transaction.TotalCost ?? 0)
                                   };

            // Sắp xếp theo tổng tiền đã mua giảm dần và lấy top 10
            var topCustomers = await topCustomersQuery
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToListAsync();

            return topCustomers;
        }

        public async Task<TopCustomerDto> GetCustomerTotalSpending(int userId, DateTime fromDate, DateTime toDate)
        {
            var toDateEnd = toDate.Date.AddDays(1).AddSeconds(-1);

            // Bao gồm các đơn hàng đã hoàn thành hoặc đã thanh toán
            var validStatuses = new List<int>
            {
                (int)TransactionStatus.done,
                (int)TransactionStatus.paidInFull,
                (int)TransactionStatus.partiallyPaid
            };

            var query = from transaction in _transactionRepository.GetQueryable()
                        join user in GetQueryable()
                        on transaction.CustomerId equals user.UserId
                        where
                            transaction.Type.Equals("Export")
                            && validStatuses.Contains((int)transaction.Status)
                            && transaction.CustomerId == userId
                            && transaction.TransactionDate >= fromDate
                            && transaction.TransactionDate <= toDateEnd
                        group new { transaction, user } by new
                        {
                            user.UserId,
                            user.FullName,
                            user.Email,
                            user.Phone,
                            user.Image,

                        } into grouped
                        select new TopCustomerDto
                        {
                            UserId = grouped.Key.UserId,
                            FullName = grouped.Key.FullName,
                            Phone = grouped.Key.Phone,
                            Email = grouped.Key.Email,
                            Image = grouped.Key.Image,
                            TotalSpent = grouped.Sum(x => x.transaction.TotalCost ?? 0),
                            NumberOfOrders = grouped.Count(),
                            AverageOrderValue = grouped.Average(x => x.transaction.TotalCost ?? 0)
                        };

            var result = await query.FirstOrDefaultAsync();
            if (result == null)
            {
                // Trả về DTO với giá trị mặc định nếu không tìm thấy
                var user = await GetByIdAsync(userId);
                return new TopCustomerDto
                {
                    UserId = userId,
                    FullName = user?.FullName ?? string.Empty,
                    Email = user?.Email ?? string.Empty,
                    Phone = user?.Phone ?? string.Empty,
                    Image = user?.Image,
                    TotalSpent = 0,
                    NumberOfOrders = 0,
                    AverageOrderValue = 0
                };
            }
            return result;
        }
    }
}
