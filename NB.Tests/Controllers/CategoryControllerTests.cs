using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.CategoryService;
using NB.Service.CategoryService.Dto;
using NB.Service.CategoryService.ViewModels;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;

namespace NB.Tests.Controllers
{
    public class CategoryControllerTests
    {
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<ILogger<CategoryController>> _mockLogger;
        private readonly CategoryController _controller;

        // Reusable Test Data Constants
        private const int ValidCategoryId = 1;
        private const int InvalidCategoryId = -1;
        private const int ZeroCategoryId = 0;
        private const int NonExistentCategoryId = 999;
        private const string ValidCategoryName = "Animal Food";
        private const string ValidCategoryDescription = "Food for animals";
        private const string ExistingCategoryName = "Existing Category";
        private const string UpdatedCategoryName = "Updated Animal Food";
        private const string UpdatedCategoryDescription = "Updated description";

        public CategoryControllerTests()
        {
            _mockCategoryService = new Mock<ICategoryService>();
            _mockProductService = new Mock<IProductService>();
            _mockInventoryService = new Mock<IInventoryService>();
            _mockLogger = new Mock<ILogger<CategoryController>>();
            _controller = new CategoryController(
                _mockCategoryService.Object,
                _mockProductService.Object,
                _mockInventoryService.Object,
                _mockLogger.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: Lay danh sach danh muc voi tim kiem hop le tra ve OK
        /// Input: CategorySearch voi CategoryName = "Electronics"
        /// Expected: HTTP 200 OK, Success = true, tra ve danh sach danh muc co phan trang
        /// </summary>
        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new CategorySearch
            {
                PageIndex = 1,
                PageSize = 10,
                CategoryName = ValidCategoryName
            };

            var categories = new List<CategoryDetailDto>
            {
                new CategoryDetailDto
                {
                    CategoryId = ValidCategoryId,
                    CategoryName = ValidCategoryName,
                    Description = ValidCategoryDescription,
                    IsActive = true
                }
            };

            _mockCategoryService
                .Setup(x => x.GetDataWithProducts())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<CategoryDetailDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(1);
            response.Data.Items.First().CategoryName.Should().Be(ValidCategoryName);
        }

        /// <summary>
        /// TCID02: Lay danh sach danh muc voi tim kiem khong tim thay tra ve NotFound
        /// Input: CategorySearch voi CategoryName = "NonExistent"
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy danh mục với tên tương tự."
        /// </summary>
        [Fact]
        public async Task GetData_WithNonExistentCategory_ReturnsNotFound()
        {
            // Arrange
            var search = new CategorySearch
            {
                PageIndex = 1,
                PageSize = 10,
                CategoryName = "NonExistent"
            };

            _mockCategoryService
                .Setup(x => x.GetDataWithProducts())
                .ReturnsAsync(new List<CategoryDetailDto>());

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy danh mục với tên tương tự.");
        }

        /// <summary>
        /// TCID03: Lay danh sach danh muc khi service nem ngoai le
        /// Input: CategorySearch hop le, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi lấy danh sách danh mục."
        /// </summary>
        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new CategorySearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockCategoryService
                .Setup(x => x.GetDataWithProducts())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy danh sách danh mục.");
        }

        #endregion

        #region GetById Tests

        /// <summary>
        /// TCID04: Lay danh muc theo ID hop le tra ve OK
        /// Input: Id = ValidCategoryId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve CategoryDetailDto voi CategoryId = 1
        /// </summary>
        [Fact]
        public async Task GetById_WithValidId_ReturnsOkWithCategory()
        {
            // Arrange
            var category = new CategoryDetailDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ValidCategoryName,
                Description = ValidCategoryDescription,
                IsActive = true
            };

            _mockCategoryService
                .Setup(x => x.GetByIdWithProducts(ValidCategoryId))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.GetById(ValidCategoryId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDetailDto?>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.CategoryId.Should().Be(ValidCategoryId);
            response.Data.CategoryName.Should().Be(ValidCategoryName);
        }

        /// <summary>
        /// TCID05: Lay danh muc voi ID khong hop le (ID <= 0)
        /// Input: Id = InvalidCategoryId (-1)
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message chứa "không hợp lệ"
        /// </summary>
        [Fact]
        public async Task GetById_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetById(InvalidCategoryId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("không hợp lệ");
        }

        /// <summary>
        /// TCID06: Lay danh muc voi ID = 0
        /// Input: Id = ZeroCategoryId (0)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetById_WithZeroId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetById(ZeroCategoryId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID07: Lay danh muc voi ID khong ton tai
        /// Input: Id = NonExistentCategoryId (999)
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message chứa "Không tìm thấy danh mục"
        /// </summary>
        [Fact]
        public async Task GetById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockCategoryService
                .Setup(x => x.GetByIdWithProducts(NonExistentCategoryId))
                .ReturnsAsync((CategoryDetailDto?)null);

            // Act
            var result = await _controller.GetById(NonExistentCategoryId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không tìm thấy danh mục");
        }

        /// <summary>
        /// TCID08: Lay danh muc khi service nem ngoai le
        /// Input: Id = ValidCategoryId (1), service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetById_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockCategoryService
                .Setup(x => x.GetByIdWithProducts(ValidCategoryId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetById(ValidCategoryId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region CreateCategory Tests

        /// <summary>
        /// TCID09: Tao danh muc moi voi du lieu hop le
        /// Input: CategoryCreateVM voi CategoryName = ValidCategoryName, Description = ValidCategoryDescription
        /// Expected: HTTP 200 OK, Success = true, tra ve CategoryDto da tao
        /// </summary>
        [Fact]
        public async Task CreateCategory_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new CategoryCreateVM
            {
                CategoryName = ValidCategoryName,
                Description = ValidCategoryDescription,
                CreatedAt = DateTime.Now
            };

            _mockCategoryService
                .Setup(x => x.GetByName(It.IsAny<string>()))
                .ReturnsAsync((CategoryDto?)null);

            _mockCategoryService
                .Setup(x => x.CreateAsync(It.IsAny<CategoryDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateCategory(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.CategoryName.Should().Be(ValidCategoryName);
            response.Data.IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TCID10: Tao danh muc voi ten da ton tai
        /// Input: CategoryCreateVM voi CategoryName = ExistingCategoryName
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Tên danh mục đã tồn tại"
        /// </summary>
        [Fact]
        public async Task CreateCategory_WithExistingName_ReturnsBadRequest()
        {
            // Arrange
            var model = new CategoryCreateVM
            {
                CategoryName = ExistingCategoryName,
                Description = ValidCategoryDescription,
                CreatedAt = DateTime.Now
            };

            var existingCategory = new CategoryDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ExistingCategoryName
            };

            _mockCategoryService
                .Setup(x => x.GetByName(It.IsAny<string>()))
                .ReturnsAsync(existingCategory);

            // Act
            var result = await _controller.CreateCategory(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Tên danh mục đã tồn tại");
        }

        /// <summary>
        /// TCID11: Tao danh muc khi service nem ngoai le
        /// Input: CategoryCreateVM hop le, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi tạo danh mục."
        /// </summary>
        [Fact]
        public async Task CreateCategory_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var model = new CategoryCreateVM
            {
                CategoryName = ValidCategoryName,
                Description = ValidCategoryDescription,
                CreatedAt = DateTime.Now
            };

            _mockCategoryService
                .Setup(x => x.GetByName(It.IsAny<string>()))
                .ReturnsAsync((CategoryDto?)null);

            _mockCategoryService
                .Setup(x => x.CreateAsync(It.IsAny<CategoryDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateCategory(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi tạo danh mục.");
        }

        #endregion

        #region UpdateCategory Tests

        /// <summary>
        /// TCID12: Cap nhat danh muc voi du lieu hop le
        /// Input: Id = ValidCategoryId (1), CategoryUpdateVM voi CategoryName = UpdatedCategoryName
        /// Expected: HTTP 200 OK, Success = true, tra ve CategoryDto da cap nhat
        /// </summary>
        [Fact]
        public async Task UpdateCategory_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new CategoryUpdateVM
            {
                CategoryName = UpdatedCategoryName,
                Description = UpdatedCategoryDescription,
                IsActive = true,
                UpdatedAt = DateTime.Now
            };

            var existingCategory = new CategoryDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ValidCategoryName,
                Description = ValidCategoryDescription,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-10)
            };

            _mockCategoryService
                .Setup(x => x.GetById(ValidCategoryId))
                .ReturnsAsync(existingCategory);

            _mockCategoryService
                .Setup(x => x.GetByName(It.IsAny<string>()))
                .ReturnsAsync((CategoryDto?)null);

            _mockCategoryService
                .Setup(x => x.UpdateAsync(It.IsAny<CategoryDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateCategory(ValidCategoryId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.CategoryName.Should().Be(UpdatedCategoryName);
        }

        /// <summary>
        /// TCID13: Cap nhat danh muc voi ID khong ton tai
        /// Input: Id = NonExistentCategoryId (999), CategoryUpdateVM hop le
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tồn tại danh mục với Id này"
        /// </summary>
        [Fact]
        public async Task UpdateCategory_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var model = new CategoryUpdateVM
            {
                CategoryName = UpdatedCategoryName,
                Description = UpdatedCategoryDescription,
                IsActive = true,
                UpdatedAt = DateTime.Now
            };

            _mockCategoryService
                .Setup(x => x.GetById(NonExistentCategoryId))
                .ReturnsAsync((CategoryDto?)null);

            // Act
            var result = await _controller.UpdateCategory(NonExistentCategoryId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tồn tại danh mục với Id này");
        }

        /// <summary>
        /// TCID14: Cap nhat danh muc voi ten da ton tai (ID khac)
        /// Input: Id = ValidCategoryId (1), CategoryUpdateVM voi CategoryName da duoc dung boi danh muc khac
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Tên danh mục này đã được đăng kí"
        /// </summary>
        [Fact]
        public async Task UpdateCategory_WithExistingNameDifferentId_ReturnsBadRequest()
        {
            // Arrange
            var model = new CategoryUpdateVM
            {
                CategoryName = ExistingCategoryName,
                Description = UpdatedCategoryDescription,
                IsActive = true,
                UpdatedAt = DateTime.Now
            };

            var existingCategory = new CategoryDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ValidCategoryName
            };

            var otherCategory = new CategoryDto
            {
                CategoryId = 2, // Different ID
                CategoryName = ExistingCategoryName
            };

            _mockCategoryService
                .Setup(x => x.GetById(ValidCategoryId))
                .ReturnsAsync(existingCategory);

            _mockCategoryService
                .Setup(x => x.GetByName(It.IsAny<string>()))
                .ReturnsAsync(otherCategory);

            // Act
            var result = await _controller.UpdateCategory(ValidCategoryId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Tên danh mục này đã được đăng kí");
        }

        /// <summary>
        /// TCID15: Cap nhat danh muc khi service nem ngoai le
        /// Input: Id = ValidCategoryId (1), CategoryUpdateVM hop le, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi cập nhật danh mục."
        /// </summary>
        [Fact]
        public async Task UpdateCategory_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var model = new CategoryUpdateVM
            {
                CategoryName = UpdatedCategoryName,
                Description = UpdatedCategoryDescription,
                IsActive = true,
                UpdatedAt = DateTime.Now
            };

            var existingCategory = new CategoryDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ValidCategoryName
            };

            _mockCategoryService
                .Setup(x => x.GetById(ValidCategoryId))
                .ReturnsAsync(existingCategory);

            _mockCategoryService
                .Setup(x => x.GetByName(It.IsAny<string>()))
                .ReturnsAsync((CategoryDto?)null);

            _mockCategoryService
                .Setup(x => x.UpdateAsync(It.IsAny<CategoryDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateCategory(ValidCategoryId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi cập nhật danh mục.");
        }

        #endregion

        #region DeleteCategory Tests

        /// <summary>
        /// TCID16: Xoa danh muc voi ID hop le (khong co san pham hoat dong)
        /// Input: Id = ValidCategoryId (1), danh muc khong co san pham IsAvailable = true
        /// Expected: HTTP 200 OK, Success = true, Data = "Xóa danh mục thành công"
        /// </summary>
        [Fact]
        public async Task DeleteCategory_WithValidIdNoActiveProducts_ReturnsOk()
        {
            // Arrange
            var category = new CategoryDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ValidCategoryName,
                IsActive = true
            };

            var inactiveProducts = new List<NB.Model.Entities.Product>
            {
                new NB.Model.Entities.Product
                {
                    ProductId = 1,
                    ProductName = "Inactive Product",
                    IsAvailable = false
                }
            };

            _mockCategoryService
                .Setup(x => x.GetById(ValidCategoryId))
                .ReturnsAsync(category);

            _mockProductService
                .Setup(x => x.GetByCategoryId(ValidCategoryId))
                .ReturnsAsync(inactiveProducts);

            _mockCategoryService
                .Setup(x => x.UpdateAsync(It.IsAny<CategoryDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteCategory(ValidCategoryId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("Xóa danh mục thành công");
        }

        /// <summary>
        /// TCID17: Xoa danh muc voi ID khong ton tai
        /// Input: Id = NonExistentCategoryId (999)
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message chứa "Không tìm thấy danh mục"
        /// </summary>
        [Fact]
        public async Task DeleteCategory_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockCategoryService
                .Setup(x => x.GetById(NonExistentCategoryId))
                .ReturnsAsync((CategoryDto?)null);

            // Act
            var result = await _controller.DeleteCategory(NonExistentCategoryId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không tìm thấy danh mục");
        }

        /// <summary>
        /// TCID18: Xoa danh muc con co san pham hoat dong
        /// Input: Id = ValidCategoryId (1), danh muc co 3 san pham IsAvailable = true
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message chứa "Không thể xóa danh mục"
        /// </summary>
        [Fact]
        public async Task DeleteCategory_WithActiveProducts_ReturnsBadRequest()
        {
            // Arrange
            var category = new CategoryDto
            {
                CategoryId = ValidCategoryId,
                CategoryName = ValidCategoryName,
                IsActive = true
            };

            var activeProducts = new List<NB.Model.Entities.Product>
            {
                new NB.Model.Entities.Product { ProductId = 1, ProductName = "Product 1", IsAvailable = true },
                new NB.Model.Entities.Product { ProductId = 2, ProductName = "Product 2", IsAvailable = true },
                new NB.Model.Entities.Product { ProductId = 3, ProductName = "Product 3", IsAvailable = true }
            };

            _mockCategoryService
                .Setup(x => x.GetById(ValidCategoryId))
                .ReturnsAsync(category);

            _mockProductService
                .Setup(x => x.GetByCategoryId(ValidCategoryId))
                .ReturnsAsync(activeProducts);

            // Act
            var result = await _controller.DeleteCategory(ValidCategoryId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không thể xóa danh mục");
            response.Error.Message.Should().Contain("3 sản phẩm");
        }

        /// <summary>
        /// TCID19: Xoa danh muc khi service nem ngoai le
        /// Input: Id = ValidCategoryId (1), service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi xóa danh mục."
        /// </summary>
        [Fact]
        public async Task DeleteCategory_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockCategoryService
                .Setup(x => x.GetById(ValidCategoryId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteCategory(ValidCategoryId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<CategoryDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi xóa danh mục.");
        }

        #endregion
    }
}
