using NB.Model.Entities;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.InventoryService
{
    public interface IInventoryService : IService<Inventory>
    {
        Task<int> GetInventoryQuantity(int warehouseId, int productId);

        Task<Inventory?> GetInventoryByWarehouseAndInventoryId(int warehouseId, int inventoryId);

        Task<bool> IsProductInWarehouse(int warehouseId, int productId);

        Task<Inventory?> GetByWarehouseAndProductId(int warehouseId, int productId);

        Task<List<Inventory>> GetAllInventoriesByProductId(int productId);
    }
}
