using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.InventoryService
{
    public class InventoryService : Service<Inventory>, IInventoryService
    {
        public InventoryService(IRepository<Inventory> serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<int> GetInventoryQuantity(int warehouseId, int productId)
        {
            var query = from i in GetQueryable()
                        where i.WarehouseId == warehouseId
                        && i.ProductId == productId
                        select i.Quantity;
            return (int)(await query.FirstOrDefaultAsync() ?? 0);
        }

        public async Task<InventoryDto?> GetByWarehouseIdAndInventoryId(int warehouseId, int inventoryId)
        {
            // Trả về InventoryDto object hoặc null
            var query = from i in GetQueryable()
                        where i.WarehouseId == warehouseId
                        && i.InventoryId == inventoryId
                        select new InventoryDto
                        {
                            InventoryId = i.InventoryId,
                            WarehouseId = i.WarehouseId,
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            LastUpdated = i.LastUpdated,
                            Product = i.Product
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> IsProductInWarehouse(int warehouseId, int productId)
        {
            // Trả về true/false (chỉ là query kiểm tra sự tồn tại)
            var query = from i in GetQueryable()
                        where i.WarehouseId == warehouseId
                        && i.ProductId == productId
                        select i;

            return await query.AnyAsync();
        }

        public async Task<bool> IsInventoryExist(int inventoryId)
        {
            // Trả về true/false (chỉ là query kiểm tra sự tồn tại)
            var query = from i in GetQueryable()
                        where i.InventoryId == inventoryId
                        select i;
            return await query.AnyAsync();
        }

        public async Task<InventoryDto?> GetByWarehouseAndProductId(int warehouseId, int productId)
        {
            var query = from i in GetQueryable()
                        where i.WarehouseId == warehouseId
                        && i.ProductId == productId
                        select new InventoryDto
                        {
                            InventoryId = i.InventoryId,
                            WarehouseId = i.WarehouseId,
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            LastUpdated = i.LastUpdated,
                            Product = i.Product
                        };
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<List<InventoryDto>> GetData()
        {
            var query = from i in GetQueryable()
                        where i.Product != null // Lọc chỉ lấy các Inventory có Product khác null
                        select new InventoryDto
                        {
                            InventoryId = i.InventoryId,
                            WarehouseId = i.WarehouseId,
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            LastUpdated = i.LastUpdated,
                            Product = i.Product
                        };
            return await query.ToListAsync();
        }

        public async Task<List<InventoryDto>> GetByProductId(int productId)
        {
            // Trả về danh sách các InventoryDto object
            var query = from i in GetQueryable()
                        where i.ProductId == productId
                        select new InventoryDto
                        {
                            InventoryId = i.InventoryId,
                            WarehouseId = i.WarehouseId,
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            LastUpdated = i.LastUpdated,
                            Product = i.Product
                        };
            return await query.ToListAsync();
        }

        public async Task<List<InventoryDto>> GetByWarehouseId(int warehouseId)
        {
            // Trả về danh sách các InventoryDto kèm theo Product entity
            var query = from i in GetQueryable()
                        where i.WarehouseId == warehouseId
                        select new InventoryDto
                        {
                            InventoryId = i.InventoryId,
                            WarehouseId = i.WarehouseId,
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            LastUpdated = i.LastUpdated,
                            Product = i.Product
                        };
            return await query.ToListAsync();
        }

        public Task<List<ProductInWarehouseDto>> GetFromList(List<InventoryDto> list)
        {
            var query = from i in list
                        where i.Product != null // Đảm bảo Product đã được load
                        select new ProductInWarehouseDto
                        {
                            // Thông tin từ Inventory
                            InventoryId = i.InventoryId,
                            LastUpdated = i.LastUpdated,
                            // Thông tin từ Product
                            ProductId = i.ProductId,
                            ProductName = i.Product.ProductName,
                            Code = i.Product.Code,
                            WeightPerUnit = i.Product.WeightPerUnit,
                        };
            return Task.FromResult(query.ToList());
        }

        public async Task<List<InventoryDto>> GetByProductIds(List<int> ids)
        {

            var query = from i in GetQueryable()
                        where ids.Contains(i.ProductId)
                        select new InventoryDto
                        {
                            InventoryId = i.InventoryId,
                            ProductId = i.ProductId,
                            WarehouseId = i.WarehouseId,
                            AverageCost = i.AverageCost,
                            Quantity = i.Quantity,
                            LastUpdated = i.LastUpdated
                        };

            return await query.ToListAsync();
        }
    }
}