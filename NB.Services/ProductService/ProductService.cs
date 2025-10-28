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

namespace NB.Service.ProductService
{
    public class ProductService : Service<Product>, IProductService
    {
        public ProductService(IRepository<Product> serviceProvider) : base(serviceProvider)
        {
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
                        where p.Code == code
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
    }
}
