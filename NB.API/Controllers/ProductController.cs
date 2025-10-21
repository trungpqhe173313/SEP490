using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService; 
using NB.Service.ProductService;
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

        [HttpGet("GetData")]
        public async Task<IActionResult> GetData(int warehouseId, int productId)
        {
            try
            {
                // 1. Lấy tổng số lượng sản phẩm (ProductService)
                var productEntity = await _productService.GetProductById(productId);

                if (productEntity == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy Sản phẩm với ID: {productId}", 404));
                }

                int totalProductStock = productEntity.StockQuantity ?? 0;

                // Lấy số lượng tồn kho (InventoryService)
                int quantityInWarehouse = await _inventoryService.GetInventoryQuantity(warehouseId, productId);

                // Tính toán số lượng còn khả dụng (Controller/Logic Layer)
                int availableQuantity = totalProductStock - quantityInWarehouse;

                // Trả về kết quả
                var result = new
                {
                    WarehouseId = warehouseId,
                    ProductId = productId,
                    TotalSystemStock = totalProductStock,
                    QuantityAllocatedInWarehouse = quantityInWarehouse,
                    AvailableQuantity = availableQuantity
                };

                return Ok(ApiResponse<object>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính số lượng khả dụng cho Kho {WarehouseId} và Sản phẩm {ProductId}", warehouseId, productId);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tính toán số lượng khả dụng."));
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