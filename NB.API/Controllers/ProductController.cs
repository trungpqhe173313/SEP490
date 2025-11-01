using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.API.Utils;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ProductService.ViewModels;
using NB.Service.SupplierService;
using NB.Service.CategoryService;
using NB.Service.UserService.Dto;

namespace NB.API.Controllers
{
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ISupplierService _supplierService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IInventoryService inventoryService,
            ISupplierService supplierService,
            ICategoryService categoryService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _supplierService = supplierService;
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] ProductSearch search)
        {
            try
            {
                var products = await _productService.GetDataWithDetails();
                var searchString = Helper.RemoveDiacritics(search.ProductName); // Chuẩn hóa chuỗi tìm kiếm

                var filteredProducts = string.IsNullOrEmpty(searchString)
                    ? products
                    : products
                        .Where(p => p.ProductName != null &&
                                    Helper.RemoveDiacritics(p.ProductName) // Chuẩn hóa tên sản phẩm
                                        .Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                if (filteredProducts.Count == 0)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm nào.", 404));
                }

                var pagedResult = PagedList<ProductDetailDto>.CreateFromList(filteredProducts, search);

                return Ok(ApiResponse<PagedList<ProductDetailDto>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm.", 400));
            }
        }

        /// <summary>
        /// Duc Anh
        /// Hàm để lấy ra danh sách các sản phẩm có sẵn cho chức năng tạo đơn khách hàng
        /// </summary>
        /// <param name="search">thêm isAvailable</param>
        /// <returns>Sanh sách sản phẩm có sẵn</returns>
        [HttpPost("GetProductAvailable")]
        public async Task<IActionResult> GetProductAvailable([FromBody] ProductSearch search)
        {
            try
            {
                //mặc định lấy available
                search.IsAvailable = true;
                //lay ra danh sach cac san pham co san
                var listProductAvailale = await _productService.GetData(search);
                //lay ra danh sach ProductId
                List<int> listProductId = listProductAvailale.Items.Select(p => p.ProductId).ToList();
                //lay ra danh sach inventory de lay so luong va gia trung binh cua san pham
                var listInventory = await _inventoryService.GetByProductIds(listProductId);
                //gắn averageCost và quantity cho product
                foreach(var p in listProductAvailale.Items)
                {
                    var inventory = listInventory.FirstOrDefault(i => i.ProductId == p.ProductId);
                    if (inventory is not null)
                    {
                        p.AverageCost = inventory.AverageCost;
                        p.Quantity = inventory.Quantity;
                    }
                }
                return Ok(ApiResponse<PagedList<ProductDto>>.Ok(listProductAvailale));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu sản phẩm");
                return BadRequest(ApiResponse<PagedList<ProductDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("GetProductsByWarehouse/{Id}")]
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
                var productList = await _productService.GetProductsByWarehouseId(Id);

                if (!productList.Any())
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm nào trong kho ID: {Id}", 404));
                }

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

            // Validate WarehouseId
            if (model.WarehouseId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"Warehouse ID {model.WarehouseId} không hợp lệ.", 400));
            }

            var warehouseInventory = await _inventoryService.GetByWarehouseId(model.WarehouseId);
            if (warehouseInventory == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tồn tại Warehouse {model.WarehouseId}.", 404));
            }
            // Validate Supplier 
            var supplier = await _supplierService.GetBySupplierId(model.SupplierId);
            if (model.SupplierId <= 0 || supplier  == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate Category           
            var category = await _categoryService.GetById(model.CategoryId);
            if (model.CategoryId <= 0 || category == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }
            // Validate Code và kiểm tra trùng
            var code = model.Code?.Trim().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ApiResponse<object>.Fail("Mã sản phẩm không được để trống.", 400));
            }

            if (await _productService.GetByCode(code) != null)
            {
                return BadRequest(ApiResponse<object>.Fail($"Mã sản phẩm {model.Code} đã tồn tại.", 400));
            }

            // Validate ProductName uniqueness
            var productName = model.ProductName?.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(ApiResponse<object>.Fail("Tên sản phẩm không được để trống.", 400));
            }

            if (await _productService.GetByProductName(productName) != null)
            {
                return BadRequest(ApiResponse<object>.Fail($"Tên sản phẩm '{model.ProductName}' đã tồn tại.", 400));
            }

            // Validate WeightPerUnit
            if (model.WeightPerUnit < 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Trọng lượng trên đơn vị phải lớn hơn hoặc bằng 0.", 400));
            }

            try
            {
                var newProductEntity = new ProductDto
                {
                    SupplierId = supplier.SupplierId,
                    CategoryId = category.CategoryId,
                    Code = code,
                    ImageUrl = model.ImageUrl?.Trim().Replace(" ", "") ?? string.Empty,
                    ProductName = productName,
                    Description = model.Description?.Trim(),
                    WeightPerUnit = model.WeightPerUnit,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _productService.CreateAsync(newProductEntity);

                var newInventoryEntity = new InventoryDto
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = newProductEntity.ProductId,
                    Quantity = 0,
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
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    WeightPerUnit = newProductEntity.WeightPerUnit,
                    CreatedAt = newProductEntity.CreatedAt
                };

                return Ok(ApiResponse<ProductOutputVM>.Ok(productOutputDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo sản phẩm mới");
                return BadRequest(ApiResponse<ProductOutputVM>.Fail("Có lỗi xảy ra khi tạo sản phẩm.", 400));
            }
        }


        [HttpPut("UpdateProduct/{Id}")]
        public async Task<IActionResult> Update(int Id, [FromBody] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            // Validate WarehouseId
            if (model.WarehouseId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"Warehouse ID {model.WarehouseId} không hợp lệ.", 400));
            }

            if (await _inventoryService.GetByWarehouseId(model.WarehouseId) == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tồn tại Warehouse {model.WarehouseId}.", 404));
            }

            // Validate Product Code uniqueness
            var newCode = model.Code?.Trim().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(newCode))
            {
                return BadRequest(ApiResponse<object>.Fail("Mã sản phẩm không được để trống.", 400));
            }

            var existingProductByCode = await _productService.GetByCode(newCode);
            if (existingProductByCode != null && existingProductByCode.ProductId != Id)
            {
                return BadRequest(ApiResponse<object>.Fail($"Mã sản phẩm {model.Code} đã tồn tại.", 400));
            }

            // Validate ProductName uniqueness
            var newProductName = model.ProductName?.Trim();
            if (string.IsNullOrWhiteSpace(newProductName))
            {
                return BadRequest(ApiResponse<object>.Fail("Tên sản phẩm không được để trống.", 400));
            }

            var existingProductByName = await _productService.GetByProductName(newProductName);
            if (existingProductByName != null && existingProductByName.ProductId != Id)
            {
                return BadRequest(ApiResponse<object>.Fail($"Tên sản phẩm '{model.ProductName}' đã tồn tại.", 400));
            }

            // Validate Supplier 
            if (model.SupplierId <= 0 || await _supplierService.GetBySupplierId(model.CategoryId) == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate Category 
            if (model.CategoryId <= 0 || await _categoryService.GetById(model.CategoryId) == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate WeightPerUnit
            if (model.WeightPerUnit.HasValue && model.WeightPerUnit < 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Trọng lượng trên đơn vị phải lớn hơn hoặc bằng 0.", 400));
            }

            // Validate Product exists
            var productEntity = await _productService.GetByIdAsync(Id);
            if (productEntity == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {Id} không tồn tại.", 404));
            }

            // Validate Product in Warehouse
            bool isInWarehouse = await _inventoryService.IsProductInWarehouse(model.WarehouseId, Id);
            if (!isInWarehouse)
            {
                return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {Id} không thuộc Warehouse ID {model.WarehouseId}.", 404));
            }

            try
            {
                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, Id);

                bool isProductChanged = false;
                bool isInventoryChanged = false;

                // Check và update từng field của Product
                if (productEntity.Code != newCode)
                {
                    productEntity.Code = newCode;
                    isProductChanged = true;
                }

                var productNameToUpdate = newProductName;
                if (!string.IsNullOrWhiteSpace(productNameToUpdate) && productEntity.ProductName != productNameToUpdate)
                {
                    productEntity.ProductName = productNameToUpdate;
                    isProductChanged = true;
                }
                var newCategory = await _categoryService.GetById(model.CategoryId);
                if (productEntity.CategoryId != newCategory.CategoryId)
                {
                    productEntity.CategoryId = newCategory.CategoryId;
                    isProductChanged = true;
                }

                var newSupplier = await _supplierService.GetBySupplierId(model.SupplierId);
                if (productEntity.SupplierId != newSupplier.SupplierId)
                {
                    productEntity.SupplierId = newSupplier.SupplierId;
                    isProductChanged = true;
                }

                var newImageUrl = model.ImageUrl?.Trim().Replace(" ", "");
                if (productEntity.ImageUrl != newImageUrl)
                {
                    productEntity.ImageUrl = newImageUrl;
                    isProductChanged = true;
                }

                var newDescription = model.Description?.Trim();
                if (productEntity.Description != newDescription)
                {
                    productEntity.Description = newDescription;
                    isProductChanged = true;
                }

                // Update IsAvailable if provided
                if (model.IsAvailable.HasValue && productEntity.IsAvailable != model.IsAvailable.Value)
                {
                    productEntity.IsAvailable = model.IsAvailable.Value;
                    isProductChanged = true;
                }

                if (productEntity.WeightPerUnit != model.WeightPerUnit)
                {
                    productEntity.WeightPerUnit = model.WeightPerUnit;
                    isProductChanged = true;
                }

                // Update Product nếu có thay đổi
                if (isProductChanged)
                {
                    productEntity.UpdatedAt = DateTime.UtcNow;
                    await _productService.UpdateAsync(productEntity);
                }

                // Update Inventory nếu có thay đổi
                if (isInventoryChanged)
                {
                    targetInventory.LastUpdated = DateTime.UtcNow;
                    await _inventoryService.UpdateAsync(targetInventory);
                }

                // Prepare response with Supplier and Category names
                ProductOutputVM result = new ProductOutputVM
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = productEntity.ProductId,
                    ProductName = productEntity.ProductName,
                    Code = productEntity.Code,
                    Description = productEntity.Description,
                    SupplierId = productEntity.SupplierId,
                    SupplierName = newSupplier.SupplierName,
                    CategoryId = productEntity.CategoryId,
                    CategoryName = newCategory.CategoryName,
                    WeightPerUnit = productEntity.WeightPerUnit,
                    CreatedAt = productEntity.CreatedAt
                };

                return Ok(ApiResponse<ProductOutputVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với Id: {Id}", Id);
                return StatusCode(500, ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật sản phẩm.", 500));
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