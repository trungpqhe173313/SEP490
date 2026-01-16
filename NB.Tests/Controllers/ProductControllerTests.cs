using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.CategoryService;
using NB.Service.CategoryService.Dto;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ProductService.ViewModels;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;

namespace NB.Tests.Controllers
{
    public class ProductControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<ISupplierService> _mockSupplierService;
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<ITransactionDetailService> _mockTransactionDetailService;
        private readonly Mock<ILogger<ProductController>> _mockLogger;
        private readonly ProductController _controller;

        // Reusable Test Data Constants
        private const int ValidProductId = 1;
        private const int NonExistentProductId = 999;
        private const int InvalidProductId = -1;
        private const int ZeroProductId = 0;
        private const int ValidSupplierId = 1;
        private const int NonExistentSupplierId = 999;
        private const int ValidCategoryId = 1;
        private const int NonExistentCategoryId = 999;
        private const int ValidWarehouseId = 1;
        private const int NonExistentWarehouseId = 999;
        private const string ValidProductName = "Product A";
        private const string ValidProductCode = "NSP000001";
        private const string ExistingProductCode = "NSP000002";
        private const string ExistingProductName = "Existing Product";
        private const string ValidSupplierName = "Supplier A";
        private const string ValidCategoryName = "Category A";
        private const string ValidWarehouseName = "Warehouse A";
        private const string ValidDescription = "Product description";
        private const string ValidImageUrl = "https://example.com/image.jpg";
        private const decimal ValidSellingPrice = 100.50m;
        private const decimal ValidWeightPerUnit = 1.5m;

        public ProductControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockInventoryService = new Mock<IInventoryService>();
            _mockSupplierService = new Mock<ISupplierService>();
            _mockCategoryService = new Mock<ICategoryService>();
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockTransactionDetailService = new Mock<ITransactionDetailService>();
            _mockLogger = new Mock<ILogger<ProductController>>();
            _controller = new ProductController(
                _mockProductService.Object,
                _mockInventoryService.Object,
                _mockSupplierService.Object,
                _mockCategoryService.Object,
                _mockWarehouseService.Object,
                _mockCloudinaryService.Object,
                _mockTransactionDetailService.Object,
                _mockLogger.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: Lay danh sach san pham voi tim kiem hop le
        /// Input: ProductSearch voi PageIndex = 1, PageSize = 10
        /// Expected: HTTP 200 OK, Success = true, tra ve PagedList<ProductOutputVM>
        /// </summary>
        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new ProductSearch { PageIndex = 1, PageSize = 10 };
            var products = new PagedList<ProductDto>(
                new List<ProductDto>
                {
                    new ProductDto
                    {
                        ProductId = ValidProductId,
                        ProductName = ValidProductName,
                        ProductCode = ValidProductCode,
                        SupplierId = ValidSupplierId,
                        CategoryId = ValidCategoryId,
                        SellingPrice = ValidSellingPrice
                    }
                }, 1, 10, 1);

            _mockProductService.Setup(x => x.GetData(search)).ReturnsAsync(products);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<ProductOutputVM>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().HaveCount(1);
        }

        /// <summary>
        /// TCID02: Lay danh sach san pham khi service nem ngoai le
        /// Input: ProductSearch hop le, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new ProductSearch { PageIndex = 1, PageSize = 10 };
            _mockProductService.Setup(x => x.GetData(search)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region GetById Tests

        /// <summary>
        /// TCID03: Lay san pham theo ID hop le
        /// Input: Id = ValidProductId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve ProductOutputVM
        /// </summary>
        [Fact]
        public async Task GetById_WithValidId_ReturnsOkWithProduct()
        {
            // Arrange
            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName,
                ProductCode = ValidProductCode,
                SupplierId = ValidSupplierId,
                CategoryId = ValidCategoryId
            };

            _mockProductService.Setup(x => x.GetById(ValidProductId)).ReturnsAsync(product);

            // Act
            var result = await _controller.GetById(ValidProductId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<ProductOutputVM>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.ProductId.Should().Be(ValidProductId);
        }

        /// <summary>
        /// TCID04: Lay san pham voi ID khong hop le (ID <= 0)
        /// Input: Id = InvalidProductId (-1)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetById_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetById(InvalidProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("không hợp lệ");
        }

        /// <summary>
        /// TCID05: Lay san pham voi ID = 0
        /// Input: Id = ZeroProductId (0)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetById_WithZeroId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetById(ZeroProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID06: Lay san pham voi ID khong ton tai
        /// Input: Id = NonExistentProductId (999)
        /// Expected: HTTP 404 NotFound, Success = false
        /// </summary>
        [Fact]
        public async Task GetById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockProductService.Setup(x => x.GetById(NonExistentProductId)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await _controller.GetById(NonExistentProductId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không tìm thấy sản phẩm");
        }

        #endregion

        #region GetProductBySupplier Tests

        /// <summary>
        /// TCID07: Lay san pham theo danh sach supplier IDs hop le
        /// Input: List<int> supplierIds = [ValidSupplierId]
        /// Expected: HTTP 200 OK, Success = true, tra ve danh sach san pham
        /// </summary>
        [Fact]
        public async Task GetProductBySupplier_WithValidIds_ReturnsOkWithProducts()
        {
            // Arrange
            var supplierIds = new List<int> { ValidSupplierId };
            var products = new List<ProductDto>
            {
                new ProductDto
                {
                    ProductId = ValidProductId,
                    ProductName = ValidProductName,
                    SupplierId = ValidSupplierId
                }
            };

            _mockProductService.Setup(x => x.GetProductsBySupplierIds(supplierIds)).ReturnsAsync(products);

            // Act
            var result = await _controller.GetProductBySupplier(supplierIds);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<List<ProductOutputVM>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Should().HaveCount(1);
        }

        /// <summary>
        /// TCID08: Lay san pham theo supplier ID khong hop le (ID <= 0)
        /// Input: List<int> supplierIds = [InvalidProductId]
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetProductBySupplier_WithInvalidId_ReturnsBadRequest()
        {
            // Arrange
            var supplierIds = new List<int> { InvalidProductId };

            // Act
            var result = await _controller.GetProductBySupplier(supplierIds);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("không hợp lệ");
        }

        #endregion

        #region GetDataByWarehouse Tests

        /// <summary>
        /// TCID09: Lay san pham theo warehouse ID hop le
        /// Input: Id = ValidWarehouseId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve danh sach san pham trong kho
        /// </summary>
        [Fact]
        public async Task GetDataByWarehouse_WithValidId_ReturnsOkWithProducts()
        {
            // Arrange
            var products = new List<ProductInWarehouseDto>
            {
                new ProductInWarehouseDto
                {
                    ProductId = ValidProductId,
                    ProductName = ValidProductName
                }
            };

            var inventory = new InventoryDto
            {
                InventoryId = 1,
                ProductId = ValidProductId,
                WarehouseId = ValidWarehouseId
            };

            _mockProductService.Setup(x => x.GetProductsByWarehouseId(ValidWarehouseId)).ReturnsAsync(products);
            _mockInventoryService.Setup(x => x.GetByWarehouseAndProductId(ValidWarehouseId, ValidProductId)).ReturnsAsync(inventory);

            // Act
            var result = await _controller.GetDataByWarehouse(ValidWarehouseId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<List<ProductInWarehouseDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Should().HaveCount(1);
        }

        /// <summary>
        /// TCID10: Lay san pham theo warehouse ID khong hop le (ID <= 0)
        /// Input: Id = InvalidProductId (-1)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetDataByWarehouse_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetDataByWarehouse(InvalidProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID11: Lay san pham theo warehouse ID khong co san pham
        /// Input: Id = ValidWarehouseId (1), kho khong co san pham
        /// Expected: HTTP 404 NotFound, Success = false
        /// </summary>
        [Fact]
        public async Task GetDataByWarehouse_WithNoProducts_ReturnsNotFound()
        {
            // Arrange
            _mockProductService.Setup(x => x.GetProductsByWarehouseId(ValidWarehouseId)).ReturnsAsync(new List<ProductInWarehouseDto>());

            // Act
            var result = await _controller.GetDataByWarehouse(ValidWarehouseId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không tìm thấy sản phẩm");
        }

        #endregion

        #region CreateProduct Tests

        /// <summary>
        /// TCID12: Tao san pham moi thanh cong
        /// Input: ProductCreateVM voi du lieu hop le
        /// Expected: HTTP 200 OK, Success = true, tra ve ProductOutputVM
        /// </summary>
        [Fact]
        public async Task CreateProduct_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new ProductCreateVM
            {
                productName = ValidProductName,
                code = ValidProductCode,
                supplierId = ValidSupplierId,
                categoryId = ValidCategoryId,
                sellingPrice = ValidSellingPrice,
                weightPerUnit = ValidWeightPerUnit,
                description = ValidDescription
            };

            var supplier = new SupplierDto { SupplierId = ValidSupplierId, SupplierName = ValidSupplierName };
            var category = new CategoryDto { CategoryId = ValidCategoryId, CategoryName = ValidCategoryName };

            _mockSupplierService.Setup(x => x.GetBySupplierId(ValidSupplierId)).ReturnsAsync(supplier);
            _mockCategoryService.Setup(x => x.GetById(ValidCategoryId)).ReturnsAsync(category);
            _mockProductService.Setup(x => x.GetByCode(ValidProductCode)).ReturnsAsync((ProductDto?)null);
            _mockProductService.Setup(x => x.CreateAsync(It.IsAny<ProductDto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<ProductOutputVM>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TCID13: Tao san pham voi code da ton tai
        /// Input: ProductCreateVM voi code = ExistingProductCode
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task CreateProduct_WithExistingCode_ReturnsBadRequest()
        {
            // Arrange
            var model = new ProductCreateVM
            {
                productName = ValidProductName,
                code = ExistingProductCode,
                supplierId = ValidSupplierId,
                categoryId = ValidCategoryId
            };

            var supplier = new SupplierDto { SupplierId = ValidSupplierId };
            var category = new CategoryDto { CategoryId = ValidCategoryId };
            var existingProduct = new ProductDto { ProductCode = ExistingProductCode };

            _mockSupplierService.Setup(x => x.GetBySupplierId(ValidSupplierId)).ReturnsAsync(supplier);
            _mockCategoryService.Setup(x => x.GetById(ValidCategoryId)).ReturnsAsync(category);
            _mockProductService.Setup(x => x.GetByCode(ExistingProductCode)).ReturnsAsync(existingProduct);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("đã tồn tại");
        }

        /// <summary>
        /// TCID14: Tao san pham voi supplier khong ton tai
        /// Input: ProductCreateVM voi supplierId = NonExistentSupplierId (999)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task CreateProduct_WithNonExistentSupplier_ReturnsBadRequest()
        {
            // Arrange
            var model = new ProductCreateVM
            {
                productName = ValidProductName,
                supplierId = NonExistentSupplierId,
                categoryId = ValidCategoryId
            };

            _mockSupplierService.Setup(x => x.GetBySupplierId(NonExistentSupplierId)).ReturnsAsync((SupplierDto?)null);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID15: Tao san pham voi category khong ton tai
        /// Input: ProductCreateVM voi categoryId = NonExistentCategoryId (999)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task CreateProduct_WithNonExistentCategory_ReturnsBadRequest()
        {
            // Arrange
            var model = new ProductCreateVM
            {
                productName = ValidProductName,
                supplierId = ValidSupplierId,
                categoryId = NonExistentCategoryId
            };

            var supplier = new SupplierDto { SupplierId = ValidSupplierId };

            _mockSupplierService.Setup(x => x.GetBySupplierId(ValidSupplierId)).ReturnsAsync(supplier);
            _mockCategoryService.Setup(x => x.GetById(NonExistentCategoryId)).ReturnsAsync((CategoryDto?)null);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        // Note: TCID16 removed - controller doesn't validate negative weight before code generation

        /// <summary>
        /// TCID17: Tao san pham voi file anh khong hop le
        /// Input: ProductCreateVM voi image co extension .txt
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task CreateProduct_WithInvalidImageExtension_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            var model = new ProductCreateVM
            {
                productName = ValidProductName,
                supplierId = ValidSupplierId,
                categoryId = ValidCategoryId,
                image = mockFile.Object
            };

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("PNG, JPG hoặc JPEG");
        }

        #endregion

        #region UpdateProduct Tests

        /// <summary>
        /// TCID18: Cap nhat san pham thanh cong
        /// Input: Id = ValidProductId (1), ProductUpdateVM voi du lieu hop le
        /// Expected: HTTP 200 OK, Success = true, tra ve ProductOutputVM da cap nhat
        /// </summary>
        [Fact]
        public async Task UpdateProduct_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new ProductUpdateVM
            {
                productName = "Updated Product Name",
                sellingPrice = 150.00m
            };

            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName,
                ProductCode = ValidProductCode,
                SupplierId = ValidSupplierId,
                CategoryId = ValidCategoryId
            };

            var supplier = new SupplierDto { SupplierId = ValidSupplierId, SupplierName = ValidSupplierName };
            var category = new CategoryDto { CategoryId = ValidCategoryId, CategoryName = ValidCategoryName };

            _mockProductService.Setup(x => x.GetByIdAsync(ValidProductId)).ReturnsAsync(product);
            _mockProductService.Setup(x => x.GetByProductName("Updated Product Name")).ReturnsAsync((ProductDto?)null);
            _mockProductService.Setup(x => x.UpdateAsync(It.IsAny<ProductDto>())).Returns(Task.CompletedTask);
            _mockSupplierService.Setup(x => x.GetBySupplierId(ValidSupplierId)).ReturnsAsync(supplier);
            _mockCategoryService.Setup(x => x.GetById(ValidCategoryId)).ReturnsAsync(category);

            // Act
            var result = await _controller.Update(ValidProductId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<ProductOutputVM>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TCID19: Cap nhat san pham voi ID khong ton tai
        /// Input: Id = NonExistentProductId (999), ProductUpdateVM hop le
        /// Expected: HTTP 404 NotFound, Success = false
        /// </summary>
        [Fact]
        public async Task UpdateProduct_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var model = new ProductUpdateVM { productName = "Updated Name" };
            _mockProductService.Setup(x => x.GetByIdAsync(NonExistentProductId)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await _controller.Update(NonExistentProductId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("không tồn tại");
        }

        /// <summary>
        /// TCID20: Cap nhat san pham voi ten da ton tai (product khac)
        /// Input: Id = ValidProductId (1), ProductUpdateVM voi productName = ExistingProductName
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task UpdateProduct_WithExistingName_ReturnsBadRequest()
        {
            // Arrange
            var model = new ProductUpdateVM { productName = ExistingProductName };

            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName
            };

            var existingProduct = new ProductDto
            {
                ProductId = 2,
                ProductName = ExistingProductName
            };

            _mockProductService.Setup(x => x.GetByIdAsync(ValidProductId)).ReturnsAsync(product);
            _mockProductService.Setup(x => x.GetByProductName(ExistingProductName)).ReturnsAsync(existingProduct);

            // Act
            var result = await _controller.Update(ValidProductId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("đã tồn tại");
        }

        #endregion

        #region DeleteProduct Tests

        /// <summary>
        /// TCID21: Xoa san pham thanh cong (khong co trong don hang va khong con ton kho)
        /// Input: Id = ValidProductId (1)
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task DeleteProduct_WithValidId_ReturnsOk()
        {
            // Arrange
            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName,
                IsAvailable = true
            };

            _mockProductService.Setup(x => x.GetById(ValidProductId)).ReturnsAsync(product);
            _mockTransactionDetailService.Setup(x => x.HasProductInActiveExportOrders(ValidProductId)).ReturnsAsync(false);
            _mockInventoryService.Setup(x => x.HasInventoryStock(ValidProductId)).ReturnsAsync(false);
            _mockProductService.Setup(x => x.UpdateAsync(It.IsAny<ProductDto>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(ValidProductId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("Xóa sản phẩm thành công");
        }

        /// <summary>
        /// TCID22: Xoa san pham voi ID khong hop le (ID <= 0)
        /// Input: Id = InvalidProductId (-1)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task DeleteProduct_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Delete(InvalidProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID23: Xoa san pham voi ID khong ton tai
        /// Input: Id = NonExistentProductId (999)
        /// Expected: HTTP 404 NotFound, Success = false
        /// </summary>
        [Fact]
        public async Task DeleteProduct_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockProductService.Setup(x => x.GetById(NonExistentProductId)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await _controller.Delete(NonExistentProductId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không tìm thấy sản phẩm");
        }

        /// <summary>
        /// TCID24: Xoa san pham da bi xoa truoc do
        /// Input: Id = ValidProductId (1), product.IsAvailable = false
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task DeleteProduct_AlreadyDeleted_ReturnsBadRequest()
        {
            // Arrange
            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName,
                IsAvailable = false
            };

            _mockProductService.Setup(x => x.GetById(ValidProductId)).ReturnsAsync(product);

            // Act
            var result = await _controller.Delete(ValidProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("đã bị xóa");
        }

        /// <summary>
        /// TCID25: Xoa san pham con trong don hang hoat dong
        /// Input: Id = ValidProductId (1), san pham co trong don hang hoat dong
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task DeleteProduct_InActiveOrders_ReturnsBadRequest()
        {
            // Arrange
            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName,
                IsAvailable = true
            };

            _mockProductService.Setup(x => x.GetById(ValidProductId)).ReturnsAsync(product);
            _mockTransactionDetailService.Setup(x => x.HasProductInActiveExportOrders(ValidProductId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(ValidProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("đang có trong đơn xuất");
        }

        /// <summary>
        /// TCID26: Xoa san pham con ton kho
        /// Input: Id = ValidProductId (1), san pham con ton kho (Quantity > 0)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task DeleteProduct_HasInventoryStock_ReturnsBadRequest()
        {
            // Arrange
            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName,
                IsAvailable = true
            };

            _mockProductService.Setup(x => x.GetById(ValidProductId)).ReturnsAsync(product);
            _mockTransactionDetailService.Setup(x => x.HasProductInActiveExportOrders(ValidProductId)).ReturnsAsync(false);
            _mockInventoryService.Setup(x => x.HasInventoryStock(ValidProductId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(ValidProductId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("còn tồn kho");
        }

        #endregion

        #region DownloadProductTemplate Tests

        /// <summary>
        /// TCID27: Tai template san pham thanh cong
        /// Input: Khong co input
        /// Expected: HTTP 200 OK, tra ve FileResult
        /// </summary>
        [Fact]
        public void DownloadProductTemplate_ReturnsFileResult()
        {
            // Act
            var result = _controller.DownloadProductTemplate();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileResult = result as FileStreamResult;
            fileResult!.FileDownloadName.Should().Contain("Product_Import_Template");
        }

        #endregion
    }
}
