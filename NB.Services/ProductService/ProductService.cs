﻿using Microsoft.EntityFrameworkCore;
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

namespace NB.Service.ProductService
{
    public class ProductService : Service<Product>, IProductService
    {
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Inventory> _inventoryRepository;

        public ProductService(
            IRepository<Product> serviceProvider,
            IRepository<Supplier> supplierRepository,
            IRepository<Category> categoryRepository,
            IRepository<Inventory> inventoryRepository) : base(serviceProvider)
        {
            _supplierRepository = supplierRepository;
            _categoryRepository = categoryRepository;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<ProductDto?> GetById(int id)
        {
            var query = from p in GetQueryable()
                        where p.ProductId == id
                        select new ProductDto
                        {
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            CategoryId = p.CategoryId,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.FirstOrDefault());
        }

        public async Task<List<ProductDto>> GetByIds(List<int> ids)
        {
            var query = from p in GetQueryable()
                        where ids.Contains(p.ProductId)
                        select new ProductDto
                        {
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            CategoryId = p.CategoryId,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.ToList());
        }

        public async Task<List<ProductDto>> GetByInventory(List<InventoryDto> list)
        {
            var productIds = list
                .Where(i => i.ProductId > 0) //validation
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            if (!productIds.Any()) //Kiểm tra danh sách rỗng
            {
                return new List<ProductDto>();
            }

            var query = from p in GetQueryable()
                        where productIds.Contains(p.ProductId)
                        select new ProductDto
                        {
                            ProductId = p.ProductId, 
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            CategoryId = p.CategoryId,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };

            return await query.ToListAsync(); 
        }

        public async Task<List<ProductDto>> GetData()
        {
            var query = from p in GetQueryable()
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            CategoryId = p.CategoryId,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await query.ToListAsync();
        }

        public async Task<ProductDto?> GetByCode(string code)
        {
            var query = from p in GetQueryable()
                        where p.Code == code.Trim().Replace(" ", "")
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            CategoryId = p.CategoryId,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.FirstOrDefault());
        }

        public async Task<List<ProductDetailDto>> GetDataWithDetails()
        {
            var query = from p in GetQueryable()
                        join s in _supplierRepository.GetQueryable() on p.SupplierId equals s.SupplierId
                        join c in _categoryRepository.GetQueryable() on p.CategoryId equals c.CategoryId
                        select new ProductDetailDto
                        {
                            ProductId = p.ProductId,
                            Code = p.Code,
                            ProductName = p.ProductName,
                            ImageUrl = p.ImageUrl,
                            WeightPerUnit = p.WeightPerUnit,
                            Description = p.Description,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt,
                            SupplierName = s.SupplierName,
                            CategoryName = c.CategoryName
                        };
            return await query.ToListAsync();
        }

        public async Task<List<ProductInWarehouseDto>> GetProductsByWarehouseId(int warehouseId)
        {
            var query = from i in _inventoryRepository.GetQueryable()
                        join p in GetQueryable() on i.ProductId equals p.ProductId
                        join s in _supplierRepository.GetQueryable() on p.SupplierId equals s.SupplierId
                        join c in _categoryRepository.GetQueryable() on p.CategoryId equals c.CategoryId
                        where i.WarehouseId == warehouseId
                        select new ProductInWarehouseDto
                        {
                            // Thông tin từ Inventory
                            InventoryId = i.InventoryId,
                            LastUpdated = i.LastUpdated,
                            // Thông tin từ Product
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            WeightPerUnit = p.WeightPerUnit,
                            Description = p.Description,
                            ImageUrl = p.ImageUrl,
                            // Thông tin từ Supplier và Category
                            SupplierName = s.SupplierName,
                            CategoryName = c.CategoryName
                        };
            return await query.ToListAsync();
        }
    }
}
