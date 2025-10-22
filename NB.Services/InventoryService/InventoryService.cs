using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
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
            var inventory = await GetQueryable() 
                                .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId 
                                && i.ProductId == productId);

            return inventory?.Quantity ?? 0;
        }
        public async Task<Inventory?> GetInventoryByWarehouseAndInventoryId(int warehouseId, int inventoryId)
        {
            // Trả về Inventory object hoặc null
            return await GetQueryable()
                         .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId 
                         && i.InventoryId == inventoryId);
        }

        public async Task<bool> IsProductInWarehouse(int warehouseId, int productId)
        {
            // Trả về true/false (chỉ là query kiểm tra sự tồn tại)
            return await GetQueryable()
                         .AnyAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        }
        public async Task<Inventory?> GetByWarehouseAndProductId(int warehouseId, int productId)
        {
           
            return await GetQueryable()
                .FirstOrDefaultAsync(
                i => i.WarehouseId == warehouseId 
                && i.ProductId == productId);
        }

        public async Task<List<Inventory>> GetAllInventoriesByProductId(int productId)
        {
            // Trả về danh sách các Inventory object
            return await GetQueryable()
                         .Where(i => i.ProductId == productId)
                         .ToListAsync();
        }

        public async Task<List<Inventory>> GetInventoriesWithProductByWarehouseId(int warehouseId)
        {
            // Service chỉ thực hiện query và include các mối quan hệ cần thiết.
            var query = from inventory in GetQueryable()
                        .Include(i => i.Product) // Include Product entity
                        where inventory.WarehouseId == warehouseId
                        select inventory;
            return await query.ToListAsync();
        }
    }
}
