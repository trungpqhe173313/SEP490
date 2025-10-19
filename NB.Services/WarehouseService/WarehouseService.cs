using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Repository.WarehouseRepository.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService
{
    public class WarehouseService : Service<Warehouse>, IWarehouseService
    {
        private readonly IRepository<Inventory> _inventoryRepository;

        public WarehouseService(
            IRepository<Warehouse> repository,
            IRepository<Inventory> inventoryRepository) : base(repository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<PagedList<WarehouseProductDto>> GetProducts(WarehouseProductSearch search)
        {
            var query = from inv in _inventoryRepository.GetQueryable()
                        select new WarehouseProductDto()
                        {
                            ProductId = inv.Product.ProductId,
                            Code = inv.Product.Code,
                            ProductName = inv.Product.ProductName,
                            Unit = inv.Product.Unit,
                            Price = inv.Product.Price,
                            StockQuantity = inv.Product.StockQuantity,
                            IsAvailable = inv.Product.IsAvailable,
                            WarehouseId = inv.Warehouse.WarehouseId,
                            WarehouseName = inv.Warehouse.WarehouseName,
                            WarehouseLocation = inv.Warehouse.Location,
                            Quantity = inv.Quantity,
                            LastUpdated = inv.LastUpdated
                        };

            query = query.OrderByDescending(p => p.ProductId);
            return await PagedList<WarehouseProductDto>.CreateAsync(query, search);
        }
    }
}
