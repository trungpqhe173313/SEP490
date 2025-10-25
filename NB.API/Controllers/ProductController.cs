using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.EmployeeService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ProductService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/products")]
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
        public async Task<IActionResult> GetData()
        {
            try
            {
                // Tạo list các inventory có Product khác null
                var productList = await _inventoryService.GetData();

                //Trả về Danh sách DTO
                return Ok(ApiResponse<List<Inventory>>.Ok(productList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm."));
            }
        }

        [HttpGet("GetProductsByWarehouse/{warehouseId}")]
        public async Task<IActionResult> GetDataByWarehouse(int warehouseId)
        {
            try
            {
                // Lấy tất cả Inventory thuộc về WarehouseId
                var inventories = await _inventoryService.GetByWarehouseId(warehouseId);

                if (!inventories.Any())
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm nào trong kho ID: {warehouseId}", 404));
                }

                // Tạo list các inventory có Product khác null
                var productList = await _inventoryService.GetFromList(inventories);

                //Trả về Danh sách DTO
                return Ok(ApiResponse<List<ProductInWarehouseDto>>.Ok(productList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho {WarehouseId}", warehouseId);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm."));
            }
        }

        [HttpPost("CreateProduct")]
        public async Task<IActionResult> Create([FromBody] ProductCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {

                // Tạo Product
                var newProductEntity = new ProductDto
                {
                    CategoryId = model.CategoryId,
                    Code = model.Code,
                    ProductName = model.ProductName,
                    SupplierId = model.SupplierId,
                    Description = model.Description,
                    WeightPerUnit = model.Weight,
                    CreatedAt = DateTime.UtcNow
                };
                await _productService.CreateAsync(newProductEntity);

                // Tạo Inventory
                var newInventoryEntity = new InventoryDto
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = newProductEntity.ProductId,
                    Quantity = model.StockQuantity,
                    LastUpdated = DateTime.UtcNow
                };
                await _inventoryService.CreateAsync(newInventoryEntity);

                var productOutputDto = new ProductOutputVM
                {
                    ProductId = newProductEntity.ProductId,
                    ProductName = newProductEntity.ProductName,
                    Code = newProductEntity.Code,
                    Description = newProductEntity.Description,
                    SupplierId = newProductEntity.SupplierId,
                    CategoryId = newProductEntity.CategoryId,
                    WeightPerUnit = newProductEntity.WeightPerUnit,
                    Quantity = newInventoryEntity.Quantity,
                    CreatedAt = newProductEntity.CreatedAt
                };

                return Ok(ApiResponse<ProductOutputVM>.Ok(productOutputDto)); 
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ProductOutputVM>.Fail(ex.Message));
            }
        }


        [HttpPut("UpdateProduct/{id}")]
        public async Task<IActionResult> Update([FromBody] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                // Kiểm tra Product có thuộc warehouse không
                bool isInWarehouse = await _inventoryService.IsProductInWarehouse(model.ProductId, model.WarehouseId);
                if (!isInWarehouse)
                {
                    return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {model.ProductId} không thuộc Warehouse ID {model.WarehouseId}."));
                }

                // Lấy entity Product và cập nhật
                var productEntity = await _productService.GetByIdAsync(model.ProductId);

                await _productService.UpdateAsync(productEntity);
                ProductDto result = new ProductDto();

                result.ProductId = productEntity.ProductId;
                result.Code = model.Code;
                result.CategoryId = model.CategoryId;
                result.ProductName = model.ProductName;
                result.Description = model.Description;
                result.WeightPerUnit = model.WeightPerUnit;
                result.ImageUrl = model.ImageUrl;
                result.IsAvailable = model.IsAvailable;


                // Lấy các Inventory thuộc Warehouse có id truyền vào và có Product được chọn
                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, model.ProductId);

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
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với Id: {Id}", model.ProductId);
                return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
            }
        }
    }
}