using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.InventoryService
{
    public class InventoryService : Service<Inventory>, IInventoryService
    {
        public InventoryService(IRepository<Inventory> repository) : base(repository)
        {
        }

        public async Task<int> GetInventoryQuantity(int warehouseId, int productId)
        {
            var query = from i in GetQueryable()
                        .Where(
                         i => i.WarehouseId == warehouseId 
                         && i.ProductId == productId)
                        select i.Quantity;
            return (int)(await query.FirstOrDefaultAsync() ?? 0);
        }
        public async Task<Inventory?> GetByWarehouseIdAndInventoryId(int warehouseId, int inventoryId)
        {
            // Trả về Inventory object hoặc null
            var query = from i in GetQueryable()
                        .Where(
                         i => i.WarehouseId == warehouseId 
                         && i.InventoryId == inventoryId)
                        select i;
            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> IsProductInWarehouse(int warehouseId, int productId)
        {
            // Trả về true/false (chỉ là query kiểm tra sự tồn tại)
            var query = from i in GetQueryable()
                        .Where(
                         i => i.WarehouseId == warehouseId 
                         && i.ProductId == productId)
                        select i;

            return await query.AnyAsync();
        }

        public async Task<bool> IsInventoryExist(int inventoryId)
        {
                       // Trả về true/false (chỉ là query kiểm tra sự tồn tại)
            var query = from i in GetQueryable()
                        .Where(
                         i => i.InventoryId == inventoryId)
                        select i;
            return await query.AnyAsync();
        }
        public async Task<Inventory?> GetByWarehouseAndProductId(int warehouseId, int productId)
        {
           
            var query = from i in GetQueryable()
                        .Where(
                         i => i.WarehouseId == warehouseId 
                         && i.ProductId == productId)
                        select i;
            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<Inventory>> GetData()
        {
            var query = from i in GetQueryable()
                        .Where(i => i.Product != null) // Lọc chỉ lấy các Inventory có Product khác null
                        select i;
            return await query.ToListAsync();
        }

        public async Task<List<Inventory>> GetByProductId(int productId)
        {
            // Trả về danh sách các Inventory object
            var query = from i in GetQueryable()
                        .Where(i => i.ProductId == productId)
                        select i;
            return await query.ToListAsync();
        }

        public async Task<List<Inventory>> GetByWarehouseId(int warehouseId)
        {
            // Trả về danh sách các Inventory kèm theo Product entity
            var query = from i in GetQueryable()
                        .Include(i => i.Product) // Include Product entity
                        where i.WarehouseId == warehouseId
                        select i;
            return await query.ToListAsync();
        }

        public async Task<List<ProductInWarehouseDto>> GetFromList(List<Inventory> list)
        {
            
            var query = list
                    .Where(i => i.Product != null) // Đảm bảo Product đã được load
                    .Select(i => new ProductInWarehouseDto
                    {
                        // Thông tin từ Inventory
                        InventoryId = i.InventoryId,
                        QuantityInStock = i.Quantity ?? 0,
                        LastUpdated = i.LastUpdated,

                        // Thông tin từ Product
                        ProductId = i.ProductId,
                        ProductName = i.Product.ProductName,
                        Code = i.Product.Code,
                        WeightPerUnit = i.Product.WeightPerUnit,
                    })
                    .ToList();
            return query.ToList();
        }
    }
}
