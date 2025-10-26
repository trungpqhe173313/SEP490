﻿using NB.Model.Entities;
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
        Task<int> GetInventoryQuantity(int warehouseId, int productId);

        Task<InventoryDto?> GetByWarehouseIdAndInventoryId(int warehouseId, int inventoryId);

        Task<bool> IsProductInWarehouse(int warehouseId, int productId);

        Task<bool> IsInventoryExist(int inventoryId);

        Task<InventoryDto?> GetByWarehouseAndProductId(int warehouseId, int productId);

        Task<List<InventoryDto>> GetData();

        Task<List<InventoryDto>> GetByProductId(int productId);

        Task<List<InventoryDto>> GetByWarehouseId(int warehouseId);

        Task<List<ProductInWarehouseDto>> GetFromList(List<InventoryDto> list);
    }
}
