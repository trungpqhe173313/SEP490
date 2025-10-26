using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.WarehouseService.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService
{
    public class WarehouseService : Service<Warehouse>,IWarehouseService
    {
        public WarehouseService(IRepository<Warehouse> serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<WarehouseDto?>> GetData()
        {
            var query = from warehouse in GetQueryable()
                        select new WarehouseDto()
                        {
                            WarehouseId = warehouse.WarehouseId,
                            WarehouseName = warehouse.WarehouseName,
                            Location = warehouse.Location,
                            Capacity = warehouse.Capacity,
                            Status = warehouse.Status,
                            Note = warehouse.Note,
                            CreatedAt = warehouse.CreatedAt
                        };
            query = query.OrderByDescending(w => w.WarehouseId);
            return await query.ToListAsync();
        }

        public async Task<WarehouseDto?> GetByWarehouseId(int search)
        {
            var query = from warehouse in GetQueryable()
                        where warehouse.WarehouseId == search
                        select new WarehouseDto()
                        {
                            WarehouseId = warehouse.WarehouseId,
                            WarehouseName = warehouse.WarehouseName,
                            Location = warehouse.Location,
                            Capacity = warehouse.Capacity,
                            Status = warehouse.Status,
                            Note = warehouse.Note,
                            CreatedAt = warehouse.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<WarehouseDto?> GetByWarehouseStatus(string status)
        {
            var query = from warehouse in GetQueryable()
                        where warehouse.Status == status
                        select new WarehouseDto()
                        {
                            WarehouseId = warehouse.WarehouseId,
                            WarehouseName = warehouse.WarehouseName,
                            Location = warehouse.Location,
                            Capacity = warehouse.Capacity,
                            Status = warehouse.Status,
                            Note = warehouse.Note,
                            CreatedAt = warehouse.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
