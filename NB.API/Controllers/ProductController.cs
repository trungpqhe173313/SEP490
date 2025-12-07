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
using NB.Service.WarehouseService;
using OfficeOpenXml;
using NB.Service.Core.Forms;

namespace NB.API.Controllers
{
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ISupplierService _supplierService;
        private readonly ICategoryService _categoryService;
        private readonly IWarehouseService _warehouseService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IInventoryService inventoryService,
            ISupplierService supplierService,
            ICategoryService categoryService,
            IWarehouseService warehouseService,
            ICloudinaryService cloudinaryService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _supplierService = supplierService;
            _categoryService = categoryService;
            _warehouseService = warehouseService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] ProductSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            
            try
            {
                
                var products = await _productService.GetData(search);
                var resultList = new List<ProductOutputVM>();
                foreach (var p in products.Items)
                {
                    resultList.Add(new ProductOutputVM
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Code = p.ProductCode,
                        Description = p.Description,
                        SupplierId = p.SupplierId,
                        SupplierName = p.SupplierName,
                        CategoryId = p.CategoryId,
                        CategoryName = p.CategoryName,
                        ImageUrl = p.ImageUrl,
                        WeightPerUnit = p.WeightPerUnit,
                        SellingPrice = p.SellingPrice,
                        IsAvailable = p.IsAvailable,
                        CreatedAt = p.CreatedAt
                    });
                }

                var pagedResult = new PagedList<ProductOutputVM>(
                    items: resultList,
                    pageIndex: products.PageIndex,
                    pageSize: products.PageSize,
                    totalCount: products.TotalCount
                );
                return Ok(ApiResponse<PagedList<ProductOutputVM>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm.", 400));
            }
        }

        [HttpGet("GetById/{Id}")]
        public async Task<IActionResult> GetById(int Id)
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
                var p = await _productService.GetById(Id);
                if (p == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm", 404));
                }
                var result = new ProductOutputVM
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Code = p.ProductCode,
                    Description = p.Description,
                    SupplierId = p.SupplierId,
                    SupplierName = p.SupplierName,
                    CategoryId = p.CategoryId,
                    CategoryName = p.CategoryName,
                    ImageUrl = p.ImageUrl,
                    WeightPerUnit = p.WeightPerUnit,
                    SellingPrice = p.SellingPrice,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt
                };
                return Ok(ApiResponse<ProductOutputVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu sản phẩm");
                return BadRequest(ApiResponse<PagedList<ProductDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpPost("GetProductBySupplier")]

        public async Task<IActionResult> GetProductBySupplier([FromBody] List<int> supplierIds)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            foreach (var id in supplierIds)
            {
                if (id <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail($"SupplierId {id} không hợp lệ", 400));
                }
            }
            try
            {
                var products = await _productService.GetProductsBySupplierIds(supplierIds);
                var resultList = new List<ProductOutputVM>(); 
                foreach (var p in products)
                {
                    resultList.Add(new ProductOutputVM
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Code = p.ProductCode,
                        Description = p.Description,
                        SupplierId = p.SupplierId,
                        SupplierName = p.SupplierName,
                        CategoryId = p.CategoryId,
                        CategoryName = p.CategoryName,
                        WeightPerUnit = p.WeightPerUnit,
                        SellingPrice = p.SellingPrice,
                        IsAvailable = p.IsAvailable,
                        CreatedAt = p.CreatedAt
                    });
                }
                return Ok(ApiResponse<List<ProductOutputVM>>.Ok(resultList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu sản phẩm theo nhà cung cấp");
                return BadRequest(ApiResponse<PagedList<ProductDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Duc Anh
        /// Hàm để lấy ra danh sách các sản phẩm có sẵn cho chức năng tạo đơn khách hàng
        /// </summary>
        /// <param name="search">thêm isAvailable</param>
        /// <returns>Danh sách sản phẩm có sẵn</returns>
        [HttpPost("GetProductAvailable")]
        public async Task<IActionResult> GetProductAvailable([FromBody] ProductSearch search)
        {
            try
            {
                // Kho tổng (mặc định Id = 1)
                int mainWarehouseId = 1;
                
                //mặc định lấy available
                search.IsAvailable = true;
                //lay ra danh sach cac san pham co san
                var listProductAvailale = await _productService.GetData(search);
                //lay ra danh sach ProductId
                List<int> listProductId = listProductAvailale.Items.Select(p => p.ProductId).ToList();
                //lay ra danh sach inventory de lay so luong va gia trung binh cua san pham trong kho tổng
                var listInventory = await _inventoryService.GetByWarehouseAndProductIds(mainWarehouseId, listProductId);
                //gắn averageCost và quantity cho product
                foreach(var p in listProductAvailale.Items)
                {
                    var inventory = listInventory.FirstOrDefault(i => i.ProductId == p.ProductId);
                    if (inventory is not null)
                    {
                        p.AverageCost = p.SellingPrice;
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

                foreach(var p in productList)
                {
                    var inventory = await _inventoryService.GetByWarehouseAndProductId(Id, p.ProductId);
                    p.InventoryId = inventory.InventoryId;
                    p.LastUpdated = inventory.LastUpdated;
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
        public async Task<IActionResult> Create([FromForm] ProductCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            // Validate image file type nếu có
            if (model.image != null)
            {
                var imageExtension = Path.GetExtension(model.image.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }
            }

            // Validate Supplier
            var supplier = await _supplierService.GetBySupplierId(model.supplierId);
            if (model.supplierId <= 0 || supplier  == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate Category
            var category = await _categoryService.GetById(model.categoryId);
            if (model.categoryId <= 0 || category == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate Code và kiểm tra trùng
            var code = model.code?.Trim().Replace(" ", "");

            // Nếu code is null/empty, tự động generate theo format NSPxxxxxx
            if (string.IsNullOrWhiteSpace(code))
            {
                code = await GenerateProductCode();
            }
            else
            {
                // Nếu có code, kiểm tra trùng
                if (await _productService.GetByCode(code) != null)
                {
                    return BadRequest(ApiResponse<object>.Fail($"Mã sản phẩm {code} đã tồn tại.", 400));
                }
            }

            // Validate ProductName uniqueness
            var productName = model.productName?.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(ApiResponse<object>.Fail("Tên sản phẩm không được để trống.", 400));
            }

            if (await _productService.GetByProductName(productName) != null)
            {
                return BadRequest(ApiResponse<object>.Fail($"Tên sản phẩm '{model.productName}' đã tồn tại.", 400));
            }

            // Validate WeightPerUnit
            if (model.weightPerUnit < 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Trọng lượng trên đơn vị phải lớn hơn hoặc bằng 0.", 400));
            }

            try
            {
                // Upload image lên Cloudinary nếu có
                string? imageUrl = null;
                if (model.image != null)
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(model.image, "products/images");
                    if (imageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                }

                var newProductEntity = new ProductDto
                {
                    SupplierId = supplier.SupplierId,
                    CategoryId = category.CategoryId,
                    ProductCode = code,
                    ImageUrl = imageUrl ?? string.Empty,
                    ProductName = productName,
                    Description = model.description?.Trim(),
                    WeightPerUnit = model.weightPerUnit,
                    SellingPrice = model.sellingPrice,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };
                await _productService.CreateAsync(newProductEntity);

                var productOutputDto = new ProductOutputVM
                {
                    ProductId = newProductEntity.ProductId,
                    ProductName = newProductEntity.ProductName,
                    Code = newProductEntity.ProductCode,
                    Description = newProductEntity.Description,
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    WeightPerUnit = newProductEntity.WeightPerUnit,
                    SellingPrice = newProductEntity.SellingPrice,
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
        public async Task<IActionResult> Update(int Id, [FromForm] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            // Validate image file type nếu có
            if (model.image != null)
            {
                var imageExtension = Path.GetExtension(model.image.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }
            }

            // Validate Product Code uniqueness
            var newCode = model.code?.Trim().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(newCode))
            {
                return BadRequest(ApiResponse<object>.Fail("Mã sản phẩm không được để trống.", 400));
            }

            var existingProductByCode = await _productService.GetByCode(newCode);
            if (existingProductByCode != null && existingProductByCode.ProductId != Id)
            {
                return BadRequest(ApiResponse<object>.Fail($"Mã sản phẩm {model.code} đã tồn tại.", 400));
            }

            // Validate ProductName uniqueness
            var newProductName = model.productName?.Trim();
            if (string.IsNullOrWhiteSpace(newProductName))
            {
                return BadRequest(ApiResponse<object>.Fail("Tên sản phẩm không được để trống.", 400));
            }

            var existingProductByName = await _productService.GetByProductName(newProductName);
            if (existingProductByName != null && existingProductByName.ProductId != Id)
            {
                return BadRequest(ApiResponse<object>.Fail($"Tên sản phẩm '{model.productName}' đã tồn tại.", 400));
            }

            // Validate Supplier
            if (model.supplierId <= 0 || await _supplierService.GetBySupplierId(model.categoryId) == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate Category
            if (model.categoryId <= 0 || await _categoryService.GetById(model.categoryId) == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh mục không được để trống.", 400));
            }

            // Validate WeightPerUnit
            if (model.weightPerUnit.HasValue && model.weightPerUnit < 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Trọng lượng trên đơn vị phải lớn hơn hoặc bằng 0.", 400));
            }

            if (model.sellingPrice.HasValue && model.sellingPrice < 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Giá bán phải lớn hơn hoặc bằng 0.", 400));
            }

            // Validate Product exists
            var productEntity = await _productService.GetByIdAsync(Id);
            if (productEntity == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {Id} không tồn tại.", 404));
            }


            try
            {
                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(model.warehouseId, Id);

                bool isProductChanged = false;
                string? oldImageUrl = productEntity.ImageUrl;

                // Check và update từng field của Product
                if (productEntity.ProductCode != newCode)
                {
                    productEntity.ProductCode = newCode;
                    isProductChanged = true;
                }

                var productNameToUpdate = newProductName;
                if (!string.IsNullOrWhiteSpace(productNameToUpdate) && productEntity.ProductName != productNameToUpdate)
                {
                    productEntity.ProductName = productNameToUpdate;
                    isProductChanged = true;
                }
                var newCategory = await _categoryService.GetById(model.categoryId);
                if (productEntity.CategoryId != newCategory.CategoryId)
                {
                    productEntity.CategoryId = newCategory.CategoryId;
                    isProductChanged = true;
                }

                var newSupplier = await _supplierService.GetBySupplierId(model.supplierId);
                if (productEntity.SupplierId != newSupplier.SupplierId)
                {
                    productEntity.SupplierId = newSupplier.SupplierId;
                    isProductChanged = true;
                }

                // Handle image update nếu có
                if (model.image != null)
                {
                    var newImageUrl = await _cloudinaryService.UpdateImageAsync(model.image, oldImageUrl, "products/images");
                    if (newImageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                    productEntity.ImageUrl = newImageUrl;
                    isProductChanged = true;
                }

                var newDescription = model.description?.Trim();
                if (productEntity.Description != newDescription)
                {
                    productEntity.Description = newDescription;
                    isProductChanged = true;
                }

                // Cập nhật IsAvailable
                if (model.isAvailable.HasValue && productEntity.IsAvailable != model.isAvailable.Value)
                {
                    productEntity.IsAvailable = model.isAvailable.Value;
                    isProductChanged = true;
                }

                if (productEntity.WeightPerUnit != model.weightPerUnit)
                {
                    productEntity.WeightPerUnit = model.weightPerUnit;
                    isProductChanged = true;
                }
                if (productEntity.SellingPrice != model.sellingPrice)
                {
                    productEntity.SellingPrice = model.sellingPrice;
                    isProductChanged = true;
                }

                // Update Product nếu có thay đổi
                if (isProductChanged)
                {
                    productEntity.UpdatedAt = DateTime.UtcNow.AddHours(7);
                    await _productService.UpdateAsync(productEntity);
                }

                // Chuẩn bị dữ liệu trả về
                ProductOutputVM result = new ProductOutputVM
                {
                    ProductId = productEntity.ProductId,
                    ProductName = productEntity.ProductName,
                    Code = productEntity.ProductCode,
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

        [HttpDelete("DeleteProduct/{Id}")]
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

        [HttpPost("ImportFromExcel")]
        public async Task<IActionResult> ImportProductsFromExcel(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<ProductImportResultVM>.Fail("File không được để trống", 400));
                }

                // Validate file extension
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(ApiResponse<ProductImportResultVM>.Fail("Chỉ chấp nhận file Excel (.xlsx, .xls)", 400));
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(ApiResponse<ProductImportResultVM>.Fail("Kích thước file không được vượt quá 10MB", 400));
                }

                var result = new ProductImportResultVM();
                var validationErrors = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        // Đọc Sheet "Nhập sản phẩm"
                        var mainSheet = package.Workbook.Worksheets.FirstOrDefault(s => s.Name == "Nhập sản phẩm");
                        if (mainSheet == null)
                        {
                            return BadRequest(ApiResponse<ProductImportResultVM>.Fail("Không tìm thấy sheet 'Nhập sản phẩm'", 400));
                        }

                        // Tìm dòng cuối cùng có dữ liệu bằng cách kiểm tra cột ProductCode (cột C)
                        // Bắt đầu từ dòng 3 (dòng đầu tiên có dữ liệu sản phẩm)
                        int lastRowWithData = 2; // Khởi tạo là 2 (trước dòng dữ liệu đầu tiên)

                        // Tìm dòng cuối cùng có ProductCode (cột 3)
                        int maxRowToCheck = mainSheet.Dimension?.Rows ?? 1000; // Giới hạn tối đa 1000 dòng để tránh vòng lặp vô hạn
                        for (int checkRow = 3; checkRow <= maxRowToCheck; checkRow++)
                        {
                            var productCodeCheck = mainSheet.Cells[checkRow, 3].Value?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(productCodeCheck))
                            {
                                lastRowWithData = checkRow;
                            }
                            else
                            {
                                // Dừng ở dòng rỗng đầu tiên
                                break;
                            }
                        }

                        // Kiểm tra có dữ liệu không
                        if (lastRowWithData < 3)
                        {
                            return BadRequest(ApiResponse<ProductImportResultVM>.Fail("Sheet 'Nhập sản phẩm' không có dữ liệu", 400));
                        }

                        result.TotalRows = lastRowWithData - 2; // Trừ 2 dòng header

                        // Class để chứa dữ liệu sản phẩm đã validate
                        var validatedProducts = new List<(
                            int rowNumber,
                            string supplierName,
                            int supplierId,
                            string categoryName,
                            int categoryId,
                            string productCode,
                            string productName,
                            decimal weightPerUnit,
                            decimal sellingPrice,
                            string? description
                        )>();

                        // Lấy ProductCode lớn nhất để làm base cho auto-generate
                        const string prefix = "NSP";
                        const int numberLength = 6;
                        var allProducts = await _productService.GetData();
                        var maxNumber = allProducts
                            .Where(p => !string.IsNullOrEmpty(p.ProductCode) &&
                                       p.ProductCode.StartsWith(prefix) &&
                                       p.ProductCode.Length == prefix.Length + numberLength)
                            .Select(p =>
                            {
                                var numberPart = p.ProductCode.Substring(prefix.Length);
                                return int.TryParse(numberPart, out int num) ? num : 0;
                            })
                            .DefaultIfEmpty(0)
                            .Max();

                        var nextAutoNumber = maxNumber + 1;
                        var generatedCodesInBatch = new HashSet<string>(); // Track codes đã generate trong batch này

                        //Vallidate sản phẩm (từ dòng 3 đến dòng cuối cùng có dữ liệu)
                        for (int row = 3; row <= lastRowWithData; row++)
                        {
                            try
                            {
                                //Đọc dữ liệu từ Excel
                                var supplierName = mainSheet.Cells[row, 1].Value?.ToString()?.Trim();      // Cột A: SupplierName
                                var categoryName = mainSheet.Cells[row, 2].Value?.ToString()?.Trim();      // Cột B: CategoryName
                                var productCode = mainSheet.Cells[row, 3].Value?.ToString()?.Trim();       // Cột C: ProductCode
                                var productName = mainSheet.Cells[row, 4].Value?.ToString()?.Trim();       // Cột D: ProductName
                                var weightPerUnitStr = mainSheet.Cells[row, 5].Value?.ToString()?.Trim(); // Cột E: WeightPerUnit
                                var sellingPriceStr = mainSheet.Cells[row, 6].Value?.ToString()?.Trim();  // Cột F: SellingPrice
                                var description = mainSheet.Cells[row, 7].Value?.ToString()?.Trim();       // Cột G: Description

                                var rowErrors = new List<string>();

                                //Validate SupplierName
                                if (string.IsNullOrWhiteSpace(supplierName))
                                {
                                    rowErrors.Add($"Dòng {row}: Tên nhà cung cấp không được để trống");
                                }

                                //Validate CategoryName
                                if (string.IsNullOrWhiteSpace(categoryName))
                                {
                                    rowErrors.Add($"Dòng {row}: Tên danh mục không được để trống");
                                }

                                //Validate ProductCode
                                if (string.IsNullOrWhiteSpace(productCode))
                                {
                                    // Tự động generate ProductCode theo format NSPxxxxxx
                                    productCode = $"{prefix}{nextAutoNumber:D6}";
                                    generatedCodesInBatch.Add(productCode);
                                    nextAutoNumber++;
                                }
                                else
                                {
                                    //Chuẩn hóa ProductCode
                                    productCode = productCode.Replace(" ", "");

                                    //Kiểm tra trùng trong DB
                                    var existingProduct = await _productService.GetByCode(productCode);
                                    if (existingProduct != null)
                                    {
                                        rowErrors.Add($"Dòng {row}: Mã sản phẩm '{productCode}' đã tồn tại trong hệ thống");
                                    }

                                    // Kiểm tra trùng trong batch hiện tại (các dòng đã xử lý trước đó)
                                    if (generatedCodesInBatch.Contains(productCode))
                                    {
                                        rowErrors.Add($"Dòng {row}: Mã sản phẩm '{productCode}' bị trùng với dòng khác trong file Excel");
                                    }
                                    else
                                    {
                                        generatedCodesInBatch.Add(productCode);
                                    }
                                }

                                //Validate ProductName
                                if (string.IsNullOrWhiteSpace(productName))
                                {
                                    rowErrors.Add($"Dòng {row}: Tên sản phẩm không được để trống");
                                }
                                else
                                {
                                    // Kiểm tra trùng trong DB
                                    var existingProduct = await _productService.GetByProductName(productName);
                                    if (existingProduct != null)
                                    {
                                        rowErrors.Add($"Dòng {row}: Tên sản phẩm '{productName}' đã tồn tại trong hệ thống");
                                    }
                                }

                                //Validate WeightPerUnit
                                decimal weightPerUnit = 0;
                                bool weightPerUnitValid = !string.IsNullOrWhiteSpace(weightPerUnitStr) &&
                                                         decimal.TryParse(weightPerUnitStr, out weightPerUnit) &&
                                                         weightPerUnit >= 0;
                                if (!weightPerUnitValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Trọng lượng trên đơn vị phải là số >= 0");
                                }

                                //Validate SellingPrice
                                decimal sellingPrice = 0;
                                bool sellingPriceValid = !string.IsNullOrWhiteSpace(sellingPriceStr) &&
                                                        decimal.TryParse(sellingPriceStr, out sellingPrice) &&
                                                        sellingPrice >= 0;
                                if (!sellingPriceValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Giá bán phải là số >= 0");
                                }

                                //Vallidate Supplier
                                var supplier = !string.IsNullOrWhiteSpace(supplierName)
                                    ? await _supplierService.GetByName(supplierName)
                                    : null;
                                if (supplier == null && !string.IsNullOrWhiteSpace(supplierName))
                                {
                                    rowErrors.Add($"Dòng {row}: Không tìm thấy nhà cung cấp với tên: {supplierName}");
                                }

                                //Vallidate Category
                                var category = !string.IsNullOrWhiteSpace(categoryName)
                                    ? await _categoryService.GetByName(categoryName)
                                    : null;
                                if (category == null && !string.IsNullOrWhiteSpace(categoryName))
                                {
                                    rowErrors.Add($"Dòng {row}: Không tìm thấy danh mục với tên: {categoryName}");
                                }

                                // Nếu có lỗi, thêm vào list và skip
                                if (rowErrors.Any())
                                {
                                    validationErrors.AddRange(rowErrors);
                                    continue;
                                }

                                // Thêm vào list đã validate
                                validatedProducts.Add((
                                    row,
                                    supplierName!,
                                    supplier!.SupplierId,
                                    categoryName!,
                                    category!.CategoryId,
                                    productCode!,
                                    productName!,
                                    weightPerUnit,
                                    sellingPrice,
                                    description
                                ));
                            }
                            catch (Exception ex)
                            {
                                validationErrors.Add($"Dòng {row}: {ex.Message}");
                            }
                        }

                        // Nếu có bất kỳ lỗi nào, return BadRequest, hủy toàn bộ quá trình import
                        if (validationErrors.Any())
                        {
                            result.TotalRows = validatedProducts.Count;
                            result.FailedCount = result.TotalRows;
                            result.SuccessCount = 0;
                            result.ErrorMessages = validationErrors;

                            return BadRequest(ApiResponse<ProductImportResultVM>.Fail(
                                validationErrors,
                                400));
                        }

                        // Nếu tất cả hợp lệ, bắt đầu tạo sản phẩm
                        foreach (var product in validatedProducts)
                        {
                            var newProduct = new ProductDto
                            {
                                SupplierId = product.supplierId,
                                CategoryId = product.categoryId,
                                ProductCode = product.productCode,
                                ProductName = product.productName,
                                WeightPerUnit = product.weightPerUnit,
                                SellingPrice = product.sellingPrice,
                                Description = product.description,
                                ImageUrl = "ImagePath",
                                IsAvailable = true,
                                CreatedAt = DateTime.UtcNow.AddHours(7),
                                UpdatedAt = DateTime.UtcNow.AddHours(7)
                            };

                            await _productService.CreateAsync(newProduct);

                            // Tạo kết quả trả về
                            var resultItem = new ProductImportedItemVM
                            {
                                ProductId = newProduct.ProductId,
                                ProductCode = newProduct.ProductCode,
                                ProductName = newProduct.ProductName,
                                SupplierName = product.supplierName,
                                CategoryName = product.categoryName,
                                WeightPerUnit = newProduct.WeightPerUnit,
                                SellingPrice = newProduct.SellingPrice,
                                Description = newProduct.Description
                            };

                            result.ImportedProducts.Add(resultItem);
                            result.SuccessCount++;
                        }
                    }
                }

                // Hoàn tất import và trả về kết quả
                result.ErrorMessages = new List<string>();
                result.FailedCount = 0;
                return Ok(ApiResponse<ProductImportResultVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import file Excel");
                return BadRequest(ApiResponse<ProductImportResultVM>.Fail($"Có lỗi xảy ra: {ex.Message}", 400));
            }
        }

        [HttpGet("DownloadProductTemplate")]
        public IActionResult DownloadProductTemplate()
        {
            try
            {
                var stream = ExcelTemplateGenerator.GenerateProductImportTemplate();
                var fileName = $"Product_Import_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo template Excel");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tạo template", 400));
            }
        }

        /// <summary>
        /// Tự động generate ProductCode theo format NSPxxxxxx
        /// Tìm ProductCode lớn nhất có format NSPxxxxxx và tăng lên 1
        /// </summary>
        /// <returns>ProductCode mới với format NSPxxxxxx (ví dụ: NSP000001)</returns>
        private async Task<string> GenerateProductCode()
        {
            const string prefix = "NSP";
            const int numberLength = 6;

            // Lấy tất cả products từ DB
            var allProducts = await _productService.GetData();

            // Filter ra các ProductCode có format NSPxxxxxx và parse số
            var maxNumber = allProducts
                .Where(p => !string.IsNullOrEmpty(p.ProductCode) &&
                           p.ProductCode.StartsWith(prefix) &&
                           p.ProductCode.Length == prefix.Length + numberLength)
                .Select(p =>
                {
                    var numberPart = p.ProductCode.Substring(prefix.Length);
                    return int.TryParse(numberPart, out int num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            // Tăng lên 1 và format lại thành NSPxxxxxx
            var nextNumber = maxNumber + 1;
            return $"{prefix}{nextNumber:D6}"; // D6 = 6 chữ số với leading zeros
        }
    }

}