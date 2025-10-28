using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.SupplierService.Dto;

namespace NB.Service.SupplierService
{
    public class SupplierService : Service<Supplier>, ISupplierService
    {
        public SupplierService(IRepository<Supplier> repository) : base(repository)
        {
        }

        public async Task<PagedList<SupplierDto>> GetData(SupplierSearch search)
        {
            var query = from sup in GetQueryable()
                        select new SupplierDto()
                        {
                            SupplierId = sup.SupplierId,
                            SupplierName = sup.SupplierName,
                            Phone = sup.Phone,
                            Email = sup.Email,
                            IsActive = sup.IsActive,
                            CreatedAt = sup.CreatedAt
                        };
            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.SupplierName))
                {
                    query = query.Where(s => s.SupplierName != null && s.SupplierName.ToLower().Contains(search.SupplierName.ToLower()));
                }
                if (!string.IsNullOrEmpty(search.Email))
                {
                    query = query.Where(s => s.Email != null && s.Email.Contains(search.Email));
                }
                if (!string.IsNullOrEmpty(search.Phone))
                {
                    query = query.Where(s => s.Phone != null && s.Phone.Contains(search.Phone));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == search.IsActive);
                }
            }
            query = query.OrderByDescending(s => s.CreatedAt);
            return await PagedList<SupplierDto>.CreateAsync(query, search);
        }

        public async Task<SupplierDto?> GetBySupplierId(int id)
        {
            var query = from sup in GetQueryable()
                        where sup.SupplierId == id
                        select new SupplierDto()
                        {
                            SupplierId = sup.SupplierId,
                            SupplierName = sup.SupplierName,
                            Phone = sup.Phone,
                            Email = sup.Email,
                            IsActive = sup.IsActive,
                            CreatedAt = sup.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<SupplierDto?> GetByEmail(string email)
        {
            var query = from sup in GetQueryable()
                        where sup.Email
                        .ToLower()
                        .Equals(email.ToLower())
                        select new SupplierDto()
                        {
                            SupplierId = sup.SupplierId,
                            SupplierName = sup.SupplierName,
                            Phone = sup.Phone,
                            Email = sup.Email,
                            IsActive = sup.IsActive,
                            CreatedAt = sup.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<SupplierDto?> GetByPhone(string phone)
        {
            var query = from sup in GetQueryable()
                        where sup.Phone == phone
                        select new SupplierDto()
                        {
                            SupplierId = sup.SupplierId,
                            SupplierName = sup.SupplierName,
                            Phone = sup.Phone,
                            Email = sup.Email,
                            IsActive = sup.IsActive,
                            CreatedAt = sup.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<SupplierDto?> GetByName(string name)
        {
            // Chuẩn hóa tên tìm kiếm: loại bỏ khoảng trắng và chuyển về lowercase
            var normalizedSearchName = name.Replace(" ", "").ToLower();

            var query = from sup in GetQueryable()
                        where sup.SupplierName.Replace(" ", "").ToLower() == normalizedSearchName
                        select new SupplierDto()
                        {
                            SupplierId = sup.SupplierId,
                            SupplierName = sup.SupplierName,
                            Phone = sup.Phone,
                            Email = sup.Email,
                            IsActive = sup.IsActive,
                            CreatedAt = sup.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
