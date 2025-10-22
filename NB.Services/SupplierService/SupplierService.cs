using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.SupplierService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService
{
    public class SupplierService : Service<Supplier>, ISupplierService
    {
        public SupplierService(IRepository<Supplier> repository) : base(repository)
        {
        }

        public Task<PagedList<SupplierDto>> GetData(SupplierSearch search)
        {
            var query = from sup in GetQueryable()
                        select new SupplierDto()
                        {
                            SupplierId = sup.SupplierId,
                            SupplierName = sup.SupplierName,
                            Phone = sup.Phone,
                            Email = sup.Email,
                            IsVerified = sup.IsVerified,
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
            }

            query = query.OrderByDescending(s => s.SupplierId);
            return PagedList<SupplierDto>.CreateAsync(query, search);
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
                            IsVerified = sup.IsVerified,
                            CreatedAt = sup.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
