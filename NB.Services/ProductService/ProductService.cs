using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService.Dto;
using NB.Service.UserService.Dto;
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
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.FirstOrDefault());
        }

        public async Task<ProductDto?> GetByProductId(int id)
        {
            var query = from p in GetQueryable()
                        where p.ProductId == id
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<ProductDto>> GetByIds(List<int> ids)
        {
            var query = from p in GetQueryable()
                        where ids.Contains(p.ProductId)
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
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
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
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
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
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
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.FirstOrDefault());
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

        public async Task<ProductDto?> GetByProductName(string productName)
        {
            // Chuẩn hóa tên tìm kiếm: loại bỏ khoảng trắng và chuyển về lowercase
            var normalizedSearchName = productName.Replace(" ", "").ToLower();

            var query = from p in GetQueryable()
                        where p.ProductName.Replace(" ", "").ToLower() == normalizedSearchName
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        //Duc Anh
        public async Task<PagedList<ProductDto>> GetData(ProductSearch search)
        {
            var baseQuery = GetQueryable()
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .AsQueryable();

            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.ProductName))
                {
                    var keyword = search.ProductName.Trim();
                    baseQuery = baseQuery.Where(u => EF.Functions.Collate(u.ProductName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.IsAvailable.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.IsAvailable == search.IsAvailable);
                }
                if (search.SupplierId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.SupplierId == search.SupplierId);
                }
            }

            var query = baseQuery.Select(p => new ProductDto()
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            Code = p.Code,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        });
            
            query = query.OrderBy(p => p.CreatedAt);
            return await PagedList<ProductDto>.CreateAsync(query, search);
        }
    }
}
