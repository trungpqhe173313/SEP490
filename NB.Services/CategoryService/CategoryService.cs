using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.CategoryService.Dto;
using NB.Service.Common;
using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService
{
    public class CategoryService : Service<Category>, ICategoryService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Supplier> _supplierRepository;

        public CategoryService(
            IRepository<Category> serviceProvider,
            IRepository<Product> productRepository,
            IRepository<Supplier> supplierRepository) : base(serviceProvider)
        {
            _productRepository = productRepository;
            _supplierRepository = supplierRepository;
        }

        public async Task<List<CategoryDto>> GetData()
        {
            var query = from category in GetQueryable()
                        select new CategoryDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt
                        };
            return await query.ToListAsync();
        }

        public async Task<CategoryDto?> GetById(int id)
        {
            var query = from category in GetQueryable()
                        where category.CategoryId == id
                        select new CategoryDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<CategoryDto?> GetByName(string name)
        {
            // Chuẩn hóa tên tìm kiếm: loại bỏ khoảng trắng và chuyển về lowercase
            var normalizedSearchName = name.Replace(" ", "");

            var query = from category in GetQueryable()
                        where category.CategoryName.Replace(" ", "") == normalizedSearchName
                        select new CategoryDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<CategoryDetailDto>> GetDataWithProducts()
        {
            var query = from category in GetQueryable()
                        select new CategoryDetailDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt,
                            Products = (from p in _productRepository.GetQueryable()
                                       join s in _supplierRepository.GetQueryable() on p.SupplierId equals s.SupplierId
                                       join c in GetQueryable() on p.CategoryId equals c.CategoryId
                                       where p.CategoryId == category.CategoryId
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
                                       }).ToList()
                        };
            return await query.ToListAsync();
        }

        public async Task<CategoryDetailDto?> GetByIdWithProducts(int id)
        {
            var query = from category in GetQueryable()
                        where category.CategoryId == id
                        select new CategoryDetailDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt,
                            Products = (from p in _productRepository.GetQueryable()
                                       join s in _supplierRepository.GetQueryable() on p.SupplierId equals s.SupplierId
                                       join c in GetQueryable() on p.CategoryId equals c.CategoryId
                                       where p.CategoryId == category.CategoryId
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
                                       }).ToList()
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
