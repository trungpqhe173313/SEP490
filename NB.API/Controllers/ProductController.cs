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
using NB.Service.SupplierService;

namespace NB.API.Controllers
{
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ISupplierService _supplierService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IInventoryService inventoryService,
            ISupplierService supplierService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _supplierService = supplierService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] ProductSearch search)
        {
            try
            {
                var products = await _productService.GetData();
                var filteredProducts = string.IsNullOrEmpty(search.ProductName)
                    ? products
                    : products
                        .Where(p => p.ProductName != null &&
                                   p.ProductName.Contains(search.ProductName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                if (filteredProducts.Count == 0)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm nào.", 404));
                }

                var pagedResult = PagedList<ProductDto>.CreateFromList(filteredProducts, search);

                return Ok(ApiResponse<PagedList<ProductDto>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm.", 400));
            }
        }

        [HttpGet("GetProductsByWarehouse/{warehouseId}")]
        public async Task<IActionResult> GetDataByWarehouse(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (Id <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"WarehouseId {Id} không hợp lệ", 400));
            }
            try
            {
                var inventories = await _inventoryService.GetByWarehouseId(Id);

                if (!inventories.Any())
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm nào trong kho ID: {Id}", 404));
                }

                var productList = await _inventoryService.GetFromList(inventories);

                return Ok(ApiResponse<List<ProductInWarehouseDto>>.Ok(productList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho {WarehouseId}", Id);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm.", 400));
            }
        }

        [HttpPost("CreateProduct")]
        public async Task<IActionResult> Create([FromBody] ProductCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (await _inventoryService.GetByWarehouseId(model.WarehouseId) == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tồn tại Warehouse {model.WarehouseId}.", 404));
            }
            if (model.CategoryId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"Category ID {model.CategoryId} không hợp lệ.", 400));
            }
            if (await _productService.GetByCode(model.Code.Replace(" ", "")) != null)
            {
                return BadRequest(ApiResponse<object>.Fail($"Mã sản phẩm {model.Code.Replace(" ", "")} đã tồn tại.", 400));
            }
            if (model.SupplierId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"Supplier ID {model.SupplierId} không hợp lệ.", 400));
            }
            var supplier = await _supplierService.GetBySupplierId(model.SupplierId);
            if (supplier == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Supplier ID {model.SupplierId} không tồn tại.", 404));
            }
            try
            {

                var newProductEntity = new ProductDto
                {
                    SupplierId = model.SupplierId,
                    CategoryId = model.CategoryId,
                    Code = model.Code.Replace(" ", ""),
                    ImageUrl = model.ImageUrl.Replace(" ", ""),
                    ProductName = model.ProductName.Replace(" ", ""),
                    Description = model.Description,
                    WeightPerUnit = model.WeightPerUnit,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _productService.CreateAsync(newProductEntity);

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
                    WarehouseId = model.WarehouseId,
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
                return BadRequest(ApiResponse<ProductOutputVM>.Fail(ex.Message, 400));
            }
        }


        [HttpPut("UpdateProduct/{Id}")]
        public async Task<IActionResult> Update(int Id, [FromBody] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (await _inventoryService.GetByWarehouseId(model.WarehouseId) == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tồn tại Warehouse {model.WarehouseId}.", 404));
            }
            var existingProductByCode = await _productService.GetByCode(model.Code.Replace(" ", ""));
            if (existingProductByCode != null && existingProductByCode.ProductId != Id)
            {
                return BadRequest(ApiResponse<object>.Fail($"Mã sản phẩm {model.Code.Replace(" ", "")} đã tồn tại.", 400));
            }
            if (model.SupplierId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"Supplier ID {model.SupplierId} không hợp lệ.", 400));
            }
            var supplier = await _supplierService.GetBySupplierId(model.SupplierId);
            if (supplier == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Supplier ID {model.SupplierId} không tồn tại.", 404));
            }
            if (model.CategoryId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"Category ID {model.CategoryId} không hợp lệ.", 400));
            }
            if (await _productService.GetById(Id) == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {Id} không tồn tại.", 404));
            }
            bool isInWarehouse = await _inventoryService.IsProductInWarehouse(model.WarehouseId, Id);
            if (!isInWarehouse)
            {
                return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {Id} không thuộc Warehouse ID {model.WarehouseId}.", 404));
            }
            try
            {

                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, Id);
                var productEntity = await _productService.GetByIdAsync(Id);

                productEntity.Code = model.Code.Replace(" ", "");
                productEntity.ProductName = model.ProductName.Replace(" ", "");
                productEntity.CategoryId = model.CategoryId;
                productEntity.SupplierId = model.SupplierId;
                productEntity.ImageUrl = model.ImageUrl.Replace(" ", "");
                productEntity.Description = model.Description;
                productEntity.IsAvailable = model.IsAvailable;
                productEntity.WeightPerUnit = model.WeightPerUnit;
                productEntity.UpdatedAt = DateTime.UtcNow;

                await _productService.UpdateAsync(productEntity);

                targetInventory.LastUpdated = model.UpdatedAt;
                targetInventory.Quantity = model.Quantity;

                await _inventoryService.UpdateAsync(targetInventory);

                ProductOutputVM result = new ProductOutputVM
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = productEntity.ProductId,
                    ProductName = productEntity.ProductName,
                    Code = productEntity.Code,
                    Description = productEntity.Description,
                    SupplierId = productEntity.SupplierId,
                    CategoryId = productEntity.CategoryId,
                    WeightPerUnit = productEntity.WeightPerUnit,
                    Quantity = targetInventory.Quantity,
                    CreatedAt = productEntity.CreatedAt,
                };

                return Ok(ApiResponse<ProductOutputVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với Id: {Id}", Id);
                return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message, 400));
            }
        }

        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (Id <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"ProductId {Id} không hợp lệ", 400));
            }
            try
            {
                var product = await _productService.GetById(Id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm", 404));
                }
                if (product.IsAvailable == false)
                {
                    return BadRequest(ApiResponse<object>.Fail("Sản phẩm đã bị xóa từ trước", 400));
                }

                product.IsAvailable = false;
                await _productService.UpdateAsync(product);
                return Ok(ApiResponse<object>.Ok("Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm với Id: {Id}", Id);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi xóa sản phẩm", 400));
            }
        }
    }

}