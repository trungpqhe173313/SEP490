using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
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
        private readonly IRepository<Transaction> _transactionRepository;
        private readonly IRepository<TransactionDetail> _transactionDetailRepository;

        public ProductService(
            IRepository<Product> serviceProvider,
            IRepository<Supplier> supplierRepository,
            IRepository<Category> categoryRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Transaction> transactionRepository,
            IRepository<TransactionDetail> transactionDetailRepository) : base(serviceProvider)
        {
            _supplierRepository = supplierRepository;
            _categoryRepository = categoryRepository;
            _inventoryRepository = inventoryRepository;
            _transactionRepository = transactionRepository;
            _transactionDetailRepository = transactionDetailRepository;
        }

        public async Task<ProductDto?> GetById(int id)
        {
            var query = from p in GetQueryable()
                        where p.ProductId == id
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
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
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
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
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
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
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
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
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await query.ToListAsync();
        }

        public async Task<ProductDto?> GetByCode(string code)
        {
            var query = from p in GetQueryable()
                        where p.ProductCode == code.Trim().Replace(" ", "")
                        select new ProductDto
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
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
                            Code = p.ProductCode,
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
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
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
                if(!string.IsNullOrEmpty(search.Code))
                {
                    var keyword = search.Code.Trim();
                    baseQuery = baseQuery.Where(u => EF.Functions.Collate(u.ProductCode, "SQL_Latin1_General_CP1_CI_AI")
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
                if(search.CategoryId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.CategoryId == search.CategoryId);
                }
                if (search.MinWeightPerUnit.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.WeightPerUnit >= search.MinWeightPerUnit);
                }
                if (search.MaxWeightPerUnit.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.WeightPerUnit <= search.MaxWeightPerUnit);
                }
                if (search.CreatedFrom.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.CreatedAt >= search.CreatedFrom);
                }
                if (search.CreatedTo.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.CreatedAt <= search.CreatedTo);
                }

            }

            var query = baseQuery.Select(p => new ProductDto()
                        {
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            ProductCode = p.ProductCode,
                            SupplierId = p.SupplierId,
                            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category != null ? p.Category.CategoryName : null,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            SellingPrice = p.SellingPrice,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        });
            
            query = query.OrderByDescending(p => p.CreatedAt);
            return await PagedList<ProductDto>.CreateAsync(query, search);
        }

        public async Task<List<ProductDto>> GetProductsBySupplierIds(List<int> supplierIds)
        {
            var query = GetQueryable()
                .Include(p => p.Supplier)    
                .Include(p => p.Category)    
                .Where(p => supplierIds.Contains(p.SupplierId))
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductCode = p.ProductCode,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.SupplierName,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.CategoryName,
                    ImageUrl = p.ImageUrl,
                    Description = p.Description,
                    WeightPerUnit = p.WeightPerUnit,
                    SellingPrice = p.SellingPrice,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                });

            return await query.ToListAsync();
        }

        public async Task<List<TopSellingProductDto>> GetTopSellingProducts(DateTime fromDate, DateTime toDate)
        {
            // Tính toán ngày kết thúc (bao gồm cả ngày cuối cùng)
            var toDateEnd = toDate.Date.AddDays(1).AddSeconds(-1);

            // Query để lấy top 10 sản phẩm bán chạy nhất
            // Bao gồm các đơn hàng đã hoàn thành hoặc đã thanh toán
            var validStatuses = new List<int>
            {
                (int)TransactionStatus.done,
                (int)TransactionStatus.paidInFull,
                (int)TransactionStatus.partiallyPaid
            };

            var topProductsQuery = from detail in _transactionDetailRepository.GetQueryable()
                                   join transaction in _transactionRepository.GetQueryable()
                                       on detail.TransactionId equals transaction.TransactionId
                                   join product in GetQueryable()
                                       on detail.ProductId equals product.ProductId
                                   where transaction.Type == "Export"
                                      && validStatuses.Contains((int)transaction.Status)
                                      && transaction.TransactionDate >= fromDate
                                      && transaction.TransactionDate <= toDateEnd
                                   group new { detail, product } by new
                                   {
                                       detail.ProductId,
                                       product.ProductName,
                                       product.ProductCode,
                                       product.ImageUrl,
                                       product.SellingPrice,
                                       product.WeightPerUnit
                                   } into grouped
                                   select new TopSellingProductDto
                                   {
                                       ProductId = grouped.Key.ProductId,
                                       ProductName = grouped.Key.ProductName ?? string.Empty,
                                       ProductCode = grouped.Key.ProductCode ?? string.Empty,
                                       ImageUrl = grouped.Key.ImageUrl,
                                       SellingPrice = grouped.Key.SellingPrice,
                                       WeightPerUnit = grouped.Key.WeightPerUnit,
                                       TotalQuantitySold = grouped.Sum(x => x.detail.Quantity),
                                       TotalRevenue = grouped.Sum(x => x.detail.Quantity * x.detail.UnitPrice),
                                       NumberOfOrders = grouped.Select(x => x.detail.TransactionId).Distinct().Count()
                                   };

            // Sắp xếp theo số lượng bán giảm dần và lấy top 10
            var topProducts = await topProductsQuery
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(10)
                .ToListAsync();

            return topProducts;
        }

        public async Task<List<Product>> GetByCategoryId(int categoryId)
        {
            // Lấy TẤT CẢ sản phẩm theo category (cả active và inactive)
            return await GetQueryable()
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }
    }
}
