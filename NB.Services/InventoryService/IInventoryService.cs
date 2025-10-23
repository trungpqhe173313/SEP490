using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ProductService.Dto;
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

        Task<Inventory?> GetByWarehouseIdAndInventoryId(int warehouseId, int inventoryId);

        Task<bool> IsProductInWarehouse(int warehouseId, int productId);

        Task<bool> IsInventoryExist(int inventoryId);

        Task<Inventory?> GetByWarehouseAndProductId(int warehouseId, int productId);

        Task<List<Inventory>> GetData();

        Task<List<Inventory>> GetByProductId(int productId);

        Task<List<Inventory>> GetByWarehouseId(int warehouseId);

        Task<List<ProductInWarehouseDto>> GetFromList(List<Inventory> list);
    }
}
