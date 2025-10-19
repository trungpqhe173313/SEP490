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

        public async Task<WarehouseDto?> GetDto(int id)
        {
            var query = from warehouse in GetQueryable()
                        where warehouse.WarehouseId == id
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

        public async Task<Warehouse> Create(Warehouse warehouse)
        {
            warehouse.CreatedAt = DateTime.Now;
            if (string.IsNullOrEmpty(warehouse.Status))
            {
                warehouse.Status = "Active";
            }
            await CreateAsync(warehouse);
            return warehouse;
        }

        public async Task<Warehouse> Update(int id, Warehouse warehouse)
        {
            var existingWarehouse = await FirstOrDefaultAsync(w => w.WarehouseId == id);
            if (existingWarehouse == null)
            {
                throw new Exception($"Không tìm thấy kho với Id {id}");
            }

            existingWarehouse.WarehouseName = warehouse.WarehouseName;
            existingWarehouse.Location = warehouse.Location;
            existingWarehouse.Capacity = warehouse.Capacity;
            existingWarehouse.Status = warehouse.Status;
            existingWarehouse.Note = warehouse.Note;

            await UpdateAsync(existingWarehouse);
            return existingWarehouse;
        }

        public async Task<bool> Delete(int id)
        {
            var warehouse = await FirstOrDefaultAsync(w => w.WarehouseId == id);
            if (warehouse == null)
            {
                throw new Exception($"Không tìm thấy kho với Id {id}");
            }

            await DeleteAsync(warehouse);
            return true;
        }
    }
}
