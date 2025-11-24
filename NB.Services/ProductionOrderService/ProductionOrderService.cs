using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.ProductionOrderService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService
{
    public class ProductionOrderService : Service<ProductionOrder>, IProductionOrderService
    {
        public ProductionOrderService(IRepository<ProductionOrder> repository) : base(repository)
        {
        }

        public async Task<PagedList<ProductionOrderDto>> GetData(ProductionOrderSearch search)
        {
            var query = from po in GetQueryable()
                        select new ProductionOrderDto
                        {
                            Id = po.Id,
                            StartDate = po.StartDate,
                            EndDate = po.EndDate,
                            Status = po.Status,
                            Note = po.Note,
                            CreatedAt = po.CreatedAt
                        };

            if (search != null)
            {
                if (search.Status.HasValue)
                {
                    query = query.Where(po => po.Status == search.Status.Value);
                }
                if (search.StartDateFrom.HasValue)
                {
                    query = query.Where(po => po.StartDate >= search.StartDateFrom.Value);
                }
                if (search.StartDateTo.HasValue)
                {
                    query = query.Where(po => po.StartDate <= search.StartDateTo.Value);
                }
                if (search.EndDateFrom.HasValue)
                {
                    query = query.Where(po => po.EndDate.HasValue && po.EndDate >= search.EndDateFrom.Value);
                }
                if (search.EndDateTo.HasValue)
                {
                    query = query.Where(po => po.EndDate.HasValue && po.EndDate <= search.EndDateTo.Value);
                }
            }

            query = query.OrderByDescending(po => po.CreatedAt);
            return await PagedList<ProductionOrderDto>.CreateAsync(query, search);
        }
    }
}
