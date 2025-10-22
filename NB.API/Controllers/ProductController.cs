using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService; 
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ProductService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService; 
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IInventoryService inventoryService, 
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _inventoryService = inventoryService; 
            _logger = logger;
        }

        [HttpGet("GetProductsByWarehouse/{warehouseId}")]
        public async Task<IActionResult> GetData(int warehouseId)
        {
            try
            {
                // 1. LOGIC: Lấy tất cả Inventory thuộc về WarehouseId, có kèm Product detail (Service)
                var inventories = await _inventoryService.GetInventoriesWithProductByWarehouseId(warehouseId);

                if (!inventories.Any())
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm nào trong kho ID: {warehouseId}", 404));
                }

                // 2. LOGIC: Ánh xạ dữ liệu sang DTOs (Controller xử lý)
                var productList = inventories
                    .Where(i => i.Product != null) // Đảm bảo Product đã được load
                    .Select(i => new ProductInWarehouseDto
                    {
                        // Thông tin từ Inventory
                        InventoryId = i.InventoryId,
                        QuantityInStock = i.Quantity ?? 0,
                        LastUpdated = i.LastUpdated,

                        // Thông tin từ Product
                        ProductId = i.ProductId,
                        ProductName = i.Product.ProductName,
                        Code = i.Product.Code,
                        Price = i.Product.Price
                    })
                    .ToList();

                // 3. Trả về Danh sách DTO
                return Ok(ApiResponse<List<ProductInWarehouseDto>>.Ok(productList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho {WarehouseId}", warehouseId);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm."));
            }
        }

        [HttpPost("CreateProduct")]
        public async Task<IActionResult> Create(int inventoryId, int warehouseId,[FromBody] ProductCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                //Kiểm tra Inventory có tồn tại trong Warehouse hay không
                if (model.InventoryId.HasValue)
                {
                    var inventory = await _inventoryService.GetInventoryByWarehouseAndInventoryId(
                        warehouseId, inventoryId);

                    if (inventory == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail($"Inventory ID {inventoryId} không thuộc Warehouse ID {warehouseId}."));
                    }
                }

                // 2. Tạo product
                var newProductEntity = new Product
                {
                    CategoryId = model.CategoryId,
                    Code = model.Code,
                    ProductName = model.ProductName,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                    //Sản phẩm được tạo có ProductId = 0 trước khi lưu.
                };

                await _productService.CreateAsync(newProductEntity); // Service thực hiện CRUD

                // Tạo entity và gọi service
                // ProductId được gán sau khi CreateAsync hoàn tất 
                var newInventoryEntity = new Inventory
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = newProductEntity.ProductId, // Lấy ID mới
                    Quantity = model.StockQuantity,
                    LastUpdated = DateTime.UtcNow
                };

                await _inventoryService.CreateAsync(newInventoryEntity); 

                return Ok(ApiResponse<Product>.Ok(newProductEntity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo sản phẩm mới");
                return BadRequest(ApiResponse<Product>.Fail(ex.Message));
            }
        }


        [HttpPut("UpdateProduct/{id}")]
        public async Task<IActionResult> Update(int warehouseId, int productId, [FromBody] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                // Kiểm tra Product có thuộc warehouse không
                bool isInWarehouse = await _inventoryService.IsProductInWarehouse(productId, warehouseId);
                if (!isInWarehouse)
                {
                    return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {productId} không thuộc Warehouse ID {model.WarehouseId}."));
                }

                // Lấy entity Product và cập nhật
                var productEntity = await _productService.GetByIdAsync(productId);

                await _productService.UpdateAsync(productEntity);
                ProductDto result = new ProductDto();

                result.ProductId = model.ProductId;
                result.CategoryId = model.CategoryId;
                result.Code = model.Code;
                result.IsAvailable = model.IsAvailable;
                result.ProductName = model.ProductName;
                result.Price = model.Price;
                result.StockQuantity = model.StockQuantity;
                result.CreatedAt = model.UpdatedAt;
                

                // Lấy các Inventory thuộc Warehouse có id truyền vào và có Product được chọn
                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, warehouseId);

                if (targetInventory != null)
                {
                    // Cập nhật các trường cần thiết (ví dụ: LastUpdated)
                    targetInventory.LastUpdated = DateTime.UtcNow;

                    await _inventoryService.UpdateAsync(targetInventory);
                }

                return Ok(ApiResponse<ProductDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với Id: {Id}", productId);
                return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
            }
        }
    }
}