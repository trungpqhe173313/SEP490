using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
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
                return Ok(ApiResponse<List<InventoryDto>>.Ok(productList));
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
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
                    SupplierId = model.SupplierId,
                    CategoryId = model.CategoryId,
                    Code = model.Code,
                    ImageUrl = model.ImageUrl,
                    ProductName = model.ProductName,
                    Description = model.Description,
                    WeightPerUnit = model.Weight,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _productService.CreateAsync(newProductEntity);

                // Tạo Inventory
                var newInventoryEntity = new InventoryDto
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = newProductEntity.ProductId,
                    Quantity = model.Quantity,
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


        [HttpPut("UpdateProduct/")]
        public async Task<IActionResult> Update([FromBody] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                if(await _productService.GetById(model.ProductId) == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {model.ProductId} không tồn tại."));
                }

                if(await _inventoryService.GetByWarehouseId(model.WarehouseId) == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tồn tại Warehouse {model.WarehouseId}."));
                }
                // Kiểm tra Product có thuộc warehouse không
                bool isInWarehouse = await _inventoryService.IsProductInWarehouse(model.WarehouseId, model.ProductId);
                if (!isInWarehouse)
                {
                    return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {model.ProductId} không thuộc Warehouse ID {model.WarehouseId}."));
                }
                if (model.CategoryId <= 0)
                {
                    return NotFound(ApiResponse<object>.Fail($"Category ID {model.CategoryId} không hợp lệ."));
                }
                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, model.ProductId);
                var productEntity = await _productService.GetByIdAsync(model.ProductId);

                productEntity.Code = model.Code;
                productEntity.ProductName = model.ProductName;
                productEntity.ImageUrl = model.ImageUrl;
                productEntity.Description = model.Description;
                productEntity.IsAvailable = model.IsAvailable;
                productEntity.CategoryId = model.CategoryId;
                productEntity.WeightPerUnit = model.WeightPerUnit;
                productEntity.UpdatedAt = DateTime.UtcNow;

                await _productService.UpdateAsync(productEntity);

                targetInventory.LastUpdated = model.UpdatedAt;
                targetInventory.Quantity = model.Quantity;

                await _inventoryService.UpdateAsync(targetInventory);

                // Chuẩn bị dữ liệu trả về
                ProductDto result = new ProductDto();
                result.Code = model.Code;
                result.ProductName = model.ProductName;
                result.ImageUrl = model.ImageUrl;
                result.Description = model.Description;
                result.IsAvailable = model.IsAvailable;
                result.CategoryId = model.CategoryId;
                result.WeightPerUnit = model.WeightPerUnit;
                result.UpdatedAt = model.UpdatedAt;

                return Ok(ApiResponse<ProductDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với Id: {Id}", model.ProductId);
                return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
            }
        }

        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm", 404));
                }

                product.IsAvailable = false;
                await _productService.UpdateAsync(product);
                return Ok(ApiResponse<object>.Ok("Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm với Id: {Id}", id);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi xóa sản phẩm"));
            }
        }
    }
}