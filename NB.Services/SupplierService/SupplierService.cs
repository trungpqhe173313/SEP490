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
                    var keyword = search.SupplierName.Trim();
                    query = query.Where(s => EF.Functions.Collate(s.SupplierName, "SQL_Latin1_General_CP1_CI_AI")
                        .Contains(keyword));
                }

                if (!string.IsNullOrEmpty(search.Email))
                {
                    var keyword = search.Email.Trim();
                    query = query.Where(s => EF.Functions.Collate(s.Email, "SQL_Latin1_General_CP1_CI_AI")
                        .Contains(keyword));
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
    }
}
