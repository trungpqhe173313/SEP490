using NB.Model.Entities;
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
    public interface IInventoryService : IService<Inventory>
    {
        Task<PagedList<ProductInventoryDto>> GetProductInventoryListAsync(InventorySearch search);
        
        Task<int> GetInventoryQuantity(int warehouseId, int productId);

        Task<InventoryDto?> GetByWarehouseIdAndInventoryId(int warehouseId, int inventoryId);

        Task<bool> IsProductInWarehouse(int warehouseId, int productId);

        Task<bool> IsInventoryExist(int inventoryId);

        Task<InventoryDto?> GetByWarehouseAndProductId(int warehouseId, int productId);

        Task<List<InventoryDto>> GetData();

        Task<List<InventoryDto>> GetByProductId(int productId);
        Task<InventoryDto?> GetByProductIdRetriveOneObject(int productId);
        Task<List<InventoryDto>> GetByProductIds(List<int> ids);
        Task<List<InventoryDto>> GetByWarehouseAndProductIds(int warehouseId, List<int> ids);
        Task<List<InventoryDto>> GetByWarehouseId(int warehouseId);

        Task<List<ProductInWarehouseDto>> GetFromList(List<InventoryDto> list);

        Task<Inventory?> GetEntityByProductIdAsync(int productId);
        Task<Inventory?> GetEntityByWarehouseAndProductIdAsync(int warehouseId, int productId);
        Task<bool> HasInventoryStock(int productId);
    }
}
