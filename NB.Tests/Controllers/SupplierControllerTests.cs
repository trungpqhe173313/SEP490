using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.SupplierService.ViewModels;

namespace NB.Tests.Controllers
{
    public class SupplierControllerTests
    {
        private readonly Mock<ISupplierService> _mockSupplierService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<Supplier>> _mockLogger;
        private readonly SupplierController _controller;

        public SupplierControllerTests()
        {
            _mockSupplierService = new Mock<ISupplierService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<Supplier>>();
            _controller = new SupplierController(
                _mockSupplierService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        #region GetData Tests

        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new SupplierSearch
            {
                PageIndex = 1,
                PageSize = 10,
                SupplierName = "Supplier 1",
                IsActive = true
            };

            var supplierList = new List<SupplierDto>
            {
                new SupplierDto
                {
                    SupplierId = 1,
                    SupplierName = "Supplier 1",
                    Email = "supplier1@example.com",
                    Phone = "0123456789",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            var pagedList = new PagedList<SupplierDto>(supplierList, 1, 1, 10);

            _mockSupplierService
                .Setup(x => x.GetData(It.IsAny<SupplierSearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<SupplierDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(1);
            response.Data.Items.First().SupplierName.Should().Be("Supplier 1");
        }

        [Fact]
        public async Task GetData_WithEmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var search = new SupplierSearch
            {
                PageIndex = 1,
                PageSize = 10,
                SupplierName = "NonExistent"
            };

            var pagedList = new PagedList<SupplierDto>(new List<SupplierDto>(), 0, 1, 10);

            _mockSupplierService
                .Setup(x => x.GetData(It.IsAny<SupplierSearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<SupplierDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new SupplierSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockSupplierService
                .Setup(x => x.GetData(It.IsAny<SupplierSearch>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<SupplierDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy dữ liệu");
        }

        #endregion

        #region GetBySupplierId Tests

        [Fact]
        public async Task GetBySupplierId_WithValidId_ReturnsOkWithSupplier()
        {
            // Arrange
            var supplierId = 1;
            var supplierDto = new SupplierDto
            {
                SupplierId = 1,
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789",
                IsActive = true
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync(supplierDto);

            // Act
            var result = await _controller.GetBySupplierId(supplierId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<SupplierDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.SupplierName.Should().Be("Supplier 1");
        }

        [Fact]
        public async Task GetBySupplierId_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var supplierId = 999;

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync((SupplierDto?)null);

            // Act
            var result = await _controller.GetBySupplierId(supplierId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<SupplierDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Không tìm thấy nhà cung cấp");
            response.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetBySupplierId_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var supplierId = 1;

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetBySupplierId(supplierId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<SupplierDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra");
        }

        #endregion

        #region CreateSupplier Tests

        [Fact]
        public async Task CreateSupplier_WithValidData_ReturnsOkWithCreatedSupplier()
        {
            // Arrange
            var model = new SupplierCreateVM
            {
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789"
            };

            var supplier = new Supplier
            {
                SupplierId = 1,
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _mockSupplierService
                .Setup(x => x.GetByEmail(It.IsAny<string>()))
                .ReturnsAsync((SupplierDto?)null);

            _mockSupplierService
                .Setup(x => x.GetByPhone(It.IsAny<string>()))
                .ReturnsAsync((SupplierDto?)null);

            _mockMapper
                .Setup(x => x.Map<SupplierCreateVM, Supplier>(It.IsAny<SupplierCreateVM>()))
                .Returns(supplier);

            _mockSupplierService
                .Setup(x => x.CreateAsync(It.IsAny<Supplier>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateSupplier(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.SupplierName.Should().Be("Supplier 1");
            response.Data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateSupplier_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var model = new SupplierCreateVM
            {
                SupplierName = "Supplier 1",
                Email = "invalid-email",
                Phone = "0123456789"
            };

            _controller.ModelState.AddModelError("Email", "Email nhà cung cấp không hợp lệ");

            // Act
            var result = await _controller.CreateSupplier(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Dữ liệu không hợp lệ");
        }

        [Fact]
        public async Task CreateSupplier_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var model = new SupplierCreateVM
            {
                SupplierName = "Supplier 1",
                Email = "existing@example.com",
                Phone = "0123456789"
            };

            var existingSupplier = new SupplierDto
            {
                SupplierId = 2,
                Email = "existing@example.com"
            };

            _mockSupplierService
                .Setup(x => x.GetByEmail("existing@example.com"))
                .ReturnsAsync(existingSupplier);

            // Act
            var result = await _controller.CreateSupplier(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Email nhà cung cấp đã tồn tại");
        }

        [Fact]
        public async Task CreateSupplier_WithExistingPhone_ReturnsBadRequest()
        {
            // Arrange
            var model = new SupplierCreateVM
            {
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0987654321"
            };

            var existingSupplier = new SupplierDto
            {
                SupplierId = 2,
                Phone = "0987654321"
            };

            _mockSupplierService
                .Setup(x => x.GetByEmail(It.IsAny<string>()))
                .ReturnsAsync((SupplierDto?)null);

            _mockSupplierService
                .Setup(x => x.GetByPhone("0987654321"))
                .ReturnsAsync(existingSupplier);

            // Act
            var result = await _controller.CreateSupplier(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Số điện thoại nhà cung cấp đã tồn tại");
        }

        [Fact]
        public async Task CreateSupplier_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var model = new SupplierCreateVM
            {
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789"
            };

            _mockSupplierService
                .Setup(x => x.GetByEmail(It.IsAny<string>()))
                .ReturnsAsync((SupplierDto?)null);

            _mockSupplierService
                .Setup(x => x.GetByPhone(It.IsAny<string>()))
                .ReturnsAsync((SupplierDto?)null);

            _mockMapper
                .Setup(x => x.Map<SupplierCreateVM, Supplier>(It.IsAny<SupplierCreateVM>()))
                .Throws(new Exception("Mapping error"));

            // Act
            var result = await _controller.CreateSupplier(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi tạo nhà cung cấp");
        }

        #endregion

        #region UpdateSupplier Tests

        [Fact]
        public async Task UpdateSupplier_WithValidData_ReturnsOkWithUpdatedSupplier()
        {
            // Arrange
            var supplierId = 1;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1 Updated",
                Email = "supplier1@example.com",
                Phone = "0123456789",
                IsActive = true
            };

            var existingSupplier = new SupplierDto
            {
                SupplierId = 1,
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789",
                IsActive = true
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync(existingSupplier);

            _mockMapper
                .Setup(x => x.Map(It.IsAny<SupplierEditVM>(), It.IsAny<SupplierDto>()))
                .Callback<SupplierEditVM, SupplierDto>((vm, dto) =>
                {
                    dto.SupplierName = vm.SupplierName;
                    dto.Email = vm.Email;
                    dto.Phone = vm.Phone;
                    dto.IsActive = vm.IsActive;
                })
                .Returns((SupplierEditVM vm, SupplierDto dto) => dto);

            _mockSupplierService
                .Setup(x => x.UpdateAsync(It.IsAny<Supplier>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.SupplierName.Should().Be("Supplier 1 Updated");
        }

        [Fact]
        public async Task UpdateSupplier_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var supplierId = 1;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1",
                Email = "invalid-email",
                Phone = "0123456789"
            };

            _controller.ModelState.AddModelError("Email", "Email nhà cung cấp không hợp lệ");

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Dữ liệu không hợp lệ");
        }

        [Fact]
        public async Task UpdateSupplier_WithNonExistentSupplier_ReturnsNotFound()
        {
            // Arrange
            var supplierId = 999;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789"
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync((SupplierDto?)null);

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Không tìm thấy nhà cung cấp");
            response.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateSupplier_WithNewExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var supplierId = 1;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1",
                Email = "existing@example.com",
                Phone = "0123456789"
            };

            var existingSupplier = new SupplierDto
            {
                SupplierId = 1,
                Email = "supplier1@example.com"
            };

            var anotherSupplier = new SupplierDto
            {
                SupplierId = 2,
                Email = "existing@example.com"
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync(existingSupplier);

            _mockSupplierService
                .Setup(x => x.GetByEmail("existing@example.com"))
                .ReturnsAsync(anotherSupplier);

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Email nhà cung cấp đã tồn tại");
        }

        [Fact]
        public async Task UpdateSupplier_WithNewExistingPhone_ReturnsBadRequest()
        {
            // Arrange
            var supplierId = 1;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0987654321"
            };

            var existingSupplier = new SupplierDto
            {
                SupplierId = 1,
                Email = "supplier1@example.com",
                Phone = "0123456789"
            };

            var anotherSupplier = new SupplierDto
            {
                SupplierId = 2,
                Phone = "0987654321"
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync(existingSupplier);

            _mockSupplierService
                .Setup(x => x.GetByPhone("0987654321"))
                .ReturnsAsync(anotherSupplier);

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Số điện thoại nhà cung cấp đã tồn tại");
        }

        [Fact]
        public async Task UpdateSupplier_WithSameEmail_ReturnsOk()
        {
            // Arrange
            var supplierId = 1;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1 Updated",
                Email = "supplier1@example.com", // Same email
                Phone = "0123456789"
            };

            var existingSupplier = new SupplierDto
            {
                SupplierId = 1,
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789"
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ReturnsAsync(existingSupplier);

            _mockMapper
                .Setup(x => x.Map(It.IsAny<SupplierEditVM>(), It.IsAny<SupplierDto>()))
                .Callback<SupplierEditVM, SupplierDto>((vm, dto) =>
                {
                    dto.SupplierName = vm.SupplierName;
                    dto.Email = vm.Email;
                    dto.Phone = vm.Phone;
                })
                .Returns((SupplierEditVM vm, SupplierDto dto) => dto);

            _mockSupplierService
                .Setup(x => x.UpdateAsync(It.IsAny<Supplier>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateSupplier_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var supplierId = 1;
            var model = new SupplierEditVM
            {
                SupplierName = "Supplier 1",
                Email = "supplier1@example.com",
                Phone = "0123456789"
            };

            _mockSupplierService
                .Setup(x => x.GetBySupplierId(supplierId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateSupplier(supplierId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi cập nhật nhà cung cấp");
        }

        #endregion

        #region DeleteSupplier Tests

        [Fact]
        public async Task DeleteSupplier_WithValidId_ReturnsOkWithTrue()
        {
            // Arrange
            var supplierId = 1;
            var supplier = new Supplier
            {
                SupplierId = 1,
                SupplierName = "Supplier 1",
                IsActive = true
            };

            _mockSupplierService
                .Setup(x => x.GetByIdAsync(supplierId))
                .ReturnsAsync(supplier);

            _mockSupplierService
                .Setup(x => x.UpdateAsync(It.IsAny<Supplier>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteSupplier(supplierId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();

            // Verify that IsActive was set to false
            _mockSupplierService.Verify(x => x.UpdateAsync(It.Is<Supplier>(s => s.IsActive == false)), Times.Once);
        }

        [Fact]
        public async Task DeleteSupplier_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var supplierId = 999;

            _mockSupplierService
                .Setup(x => x.GetByIdAsync(supplierId))
                .ReturnsAsync((Supplier?)null);

            // Act
            var result = await _controller.DeleteSupplier(supplierId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Không tìm thấy nhà cung cấp");
        }

        [Fact]
        public async Task DeleteSupplier_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var supplierId = 1;

            _mockSupplierService
                .Setup(x => x.GetByIdAsync(supplierId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteSupplier(supplierId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<Supplier>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi xóa nhà cung cấp");
        }

        #endregion
    }
}
