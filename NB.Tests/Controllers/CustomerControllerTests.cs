using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.Common;
using NB.Service.CustomerService;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;

namespace NB.Tests.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> _mockCustomerService;
        private readonly Mock<ILogger<CustomerController>> _mockLogger;
        private readonly CustomerController _controller;

        public CustomerControllerTests()
        {
            _mockCustomerService = new Mock<ICustomerService>();
            _mockLogger = new Mock<ILogger<CustomerController>>();
            _controller = new CustomerController(_mockCustomerService.Object, _mockLogger.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: Kiểm tra GetData trả về OK với danh sách phân trang khi tìm kiếm hợp lệ
        /// Input: UserSearch với PageIndex = 1, PageSize = 10, FullName = "test"
        /// Expected: HTTP 200 OK, Success = true, trả về PagedList với 1 UserDto có Username = "customer1"
        /// </summary>
        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new UserSearch
            {
                PageIndex = 1,
                PageSize = 10,
                FullName = "test"
            };

            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = 1,
                    Username = "customer1",
                    FullName = "Customer One",
                    Email = "customer1@example.com"
                }
            };

            var pagedList = new PagedList<UserDto>(users, 1, 1, 10);

            _mockCustomerService
                .Setup(x => x.GetCustomersAsync(It.IsAny<UserSearch>(), false))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(1);
            response.Data.Items.First().Username.Should().Be("customer1");
        }

        /// <summary>
        /// TCID02: Kiểm tra GetData trả về BadRequest khi service throw exception
        /// Input: UserSearch hợp lệ nhưng service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi lấy dữ liệu"
        /// </summary>
        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new UserSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockCustomerService
                .Setup(x => x.GetCustomersAsync(It.IsAny<UserSearch>(), false))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy dữ liệu");
        }

        #endregion

        #region GetDataForAdmin Tests

        /// <summary>
        /// TCID03: Kiểm tra GetDataForAdmin trả về OK với danh sách phân trang khi tìm kiếm hợp lệ
        /// Input: UserSearch với PageIndex = 1, PageSize = 10
        /// Expected: HTTP 200 OK, Success = true, trả về PagedList với danh sách khách hàng cho admin
        /// </summary>
        [Fact]
        public async Task GetDataForAdmin_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new UserSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = 1,
                    Username = "customer1",
                    FullName = "Customer One"
                }
            };

            var pagedList = new PagedList<UserDto>(users, 1, 1, 10);

            _mockCustomerService
                .Setup(x => x.GetCustomersAsync(It.IsAny<UserSearch>(), true))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetDataForAdmin(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TCID04: Kiểm tra GetDataForAdmin trả về BadRequest khi service throw exception
        /// Input: UserSearch hợp lệ nhưng service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetDataForAdmin_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new UserSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockCustomerService
                .Setup(x => x.GetCustomersAsync(It.IsAny<UserSearch>(), true))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDataForAdmin(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region GetByUserId Tests

        /// <summary>
        /// TCID05: Kiểm tra GetByUserId trả về OK với thông tin khách hàng khi ID hợp lệ
        /// Input: UserId = 1
        /// Expected: HTTP 200 OK, Success = true, trả về UserDto với UserId = 1
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithValidId_ReturnsOkWithUser()
        {
            // Arrange
            var userId = 1;
            var user = new UserDto
            {
                UserId = userId,
                Username = "customer1",
                FullName = "Customer One",
                Email = "customer1@example.com"
            };

            _mockCustomerService
                .Setup(x => x.GetCustomerByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetByUserId(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.UserId.Should().Be(userId);
        }

        /// <summary>
        /// TCID06: Kiểm tra GetByUserId trả về NotFound khi ID không tồn tại
        /// Input: UserId = 999 không tồn tại
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy khách hàng"
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;

            _mockCustomerService
                .Setup(x => x.GetCustomerByIdAsync(userId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetByUserId(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Không tìm thấy khách hàng");
        }

        /// <summary>
        /// TCID07: Kiểm tra GetByUserId trả về BadRequest khi service throw InvalidOperationException
        /// Input: UserId = 1, service throw InvalidOperationException
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Invalid operation"
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var userId = 1;

            _mockCustomerService
                .Setup(x => x.GetCustomerByIdAsync(userId))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.GetByUserId(userId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Invalid operation");
        }

        /// <summary>
        /// TCID08: Kiểm tra GetByUserId trả về BadRequest khi service throw exception
        /// Input: UserId = 1, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra"
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithGenericException_ReturnsBadRequest()
        {
            // Arrange
            var userId = 1;

            _mockCustomerService
                .Setup(x => x.GetCustomerByIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetByUserId(userId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra");
        }

        #endregion

        #region UpdateCustomer Tests

        /// <summary>
        /// TCID09: Kiểm tra UpdateCustomer trả về OK khi dữ liệu hợp lệ
        /// Input: UserId = 1, UserEditVM với FullName, Email, Phone hợp lệ
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task UpdateCustomer_WithValidData_ReturnsOk()
        {
            // Arrange
            var userId = 1;
            var model = new UserEditVM
            {
                FullName = "Updated Customer",
                Email = "updated@example.com",
                Phone = "0123456789"
            };

            var updatedUser = new UserDto
            {
                UserId = userId,
                FullName = model.FullName,
                Email = model.Email
            };

            _mockCustomerService
                .Setup(x => x.UpdateCustomerAsync(userId, model, null))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.UpdateCustomer(userId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID10: Kiểm tra UpdateCustomer trả về BadRequest khi định dạng ảnh không hợp lệ
        /// Input: UserId = 1, UserEditVM với Image có extension = .txt
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message chứa "File ảnh phải có định dạng PNG, JPG hoặc JPEG"
        /// </summary>
        [Fact]
        public async Task UpdateCustomer_WithInvalidImageExtension_ReturnsBadRequest()
        {
            // Arrange
            var userId = 1;
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            var model = new UserEditVM
            {
                FullName = "Updated Customer",
                Image = mockFile.Object
            };

            // Act
            var result = await _controller.UpdateCustomer(userId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("File ảnh phải có định dạng PNG, JPG hoặc JPEG");
        }

        /// <summary>
        /// TCID11: Kiểm tra UpdateCustomer trả về NotFound khi service throw KeyNotFoundException
        /// Input: UserId = 999 không tồn tại, service throw KeyNotFoundException
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Customer not found"
        /// </summary>
        [Fact]
        public async Task UpdateCustomer_WithKeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;
            var model = new UserEditVM
            {
                FullName = "Updated Customer"
            };

            _mockCustomerService
                .Setup(x => x.UpdateCustomerAsync(userId, model, null))
                .ThrowsAsync(new KeyNotFoundException("Customer not found"));

            // Act
            var result = await _controller.UpdateCustomer(userId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Customer not found");
        }

        /// <summary>
        /// TCID12: Kiểm tra UpdateCustomer trả về BadRequest khi service throw InvalidOperationException
        /// Input: UserId = 1, service throw InvalidOperationException
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Invalid operation"
        /// </summary>
        [Fact]
        public async Task UpdateCustomer_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var userId = 1;
            var model = new UserEditVM
            {
                FullName = "Updated Customer"
            };

            _mockCustomerService
                .Setup(x => x.UpdateCustomerAsync(userId, model, null))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.UpdateCustomer(userId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Invalid operation");
        }

        /// <summary>
        /// TCID13: Kiểm tra UpdateCustomer trả về BadRequest khi service throw exception
        /// Input: UserId = 1, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi cập nhật khách hàng"
        /// </summary>
        [Fact]
        public async Task UpdateCustomer_WithGenericException_ReturnsBadRequest()
        {
            // Arrange
            var userId = 1;
            var model = new UserEditVM
            {
                FullName = "Updated Customer"
            };

            _mockCustomerService
                .Setup(x => x.UpdateCustomerAsync(userId, model, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateCustomer(userId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi cập nhật khách hàng");
        }

        #endregion

        #region DeleteCustomer Tests

        /// <summary>
        /// TCID14: Kiểm tra DeleteCustomer trả về OK khi ID khách hàng hợp lệ
        /// Input: UserId = 1
        /// Expected: HTTP 200 OK, Success = true, Data = true
        /// </summary>
        [Fact]
        public async Task DeleteCustomer_WithValidId_ReturnsOk()
        {
            // Arrange
            var userId = 1;

            _mockCustomerService
                .Setup(x => x.DeleteCustomerAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteCustomer(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        /// <summary>
        /// TCID15: Kiểm tra DeleteCustomer trả về NotFound khi ID không tồn tại
        /// Input: UserId = 999 không tồn tại, service throw KeyNotFoundException
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Customer not found"
        /// </summary>
        [Fact]
        public async Task DeleteCustomer_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;

            _mockCustomerService
                .Setup(x => x.DeleteCustomerAsync(userId))
                .ThrowsAsync(new KeyNotFoundException("Customer not found"));

            // Act
            var result = await _controller.DeleteCustomer(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Customer not found");
        }

        /// <summary>
        /// TCID16: Kiểm tra DeleteCustomer trả về BadRequest khi service throw exception
        /// Input: UserId = 1, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi xóa khách hàng"
        /// </summary>
        [Fact]
        public async Task DeleteCustomer_WithGenericException_ReturnsBadRequest()
        {
            // Arrange
            var userId = 1;

            _mockCustomerService
                .Setup(x => x.DeleteCustomerAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteCustomer(userId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi xóa khách hàng");
        }

        #endregion

        #region CreateCustomerAccount Tests

        /// <summary>
        /// TCID17: Kiểm tra CreateCustomerAccount trả về OK khi dữ liệu hợp lệ
        /// Input: CreateCustomerAccountVM với username, fullName, email, phone hợp lệ
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task CreateCustomerAccount_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new CreateCustomerAccountVM
            {
                username = "newcustomer",
                fullName = "New Customer",
                email = "newcustomer@example.com",
                phone = "0123456789"
            };

            _mockCustomerService
                .Setup(x => x.CreateCustomerAccountAsync(model, null))
                .ReturnsAsync("Customer account created successfully");

            // Act
            var result = await _controller.CreateCustomerAccount(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID18: Kiểm tra CreateCustomerAccount trả về BadRequest khi username đã tồn tại
        /// Input: CreateCustomerAccountVM với username đã tồn tại, service throw InvalidOperationException
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Username already exists"
        /// </summary>
        [Fact]
        public async Task CreateCustomerAccount_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var model = new CreateCustomerAccountVM
            {
                username = "existingcustomer",
                fullName = "Existing Customer",
                email = "existing@example.com",
                phone = "0123456789"
            };

            _mockCustomerService
                .Setup(x => x.CreateCustomerAccountAsync(model, null))
                .ThrowsAsync(new InvalidOperationException("Username already exists"));

            // Act
            var result = await _controller.CreateCustomerAccount(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Username already exists");
        }

        /// <summary>
        /// TCID19: Kiểm tra CreateCustomerAccount trả về BadRequest khi service throw exception
        /// Input: CreateCustomerAccountVM hợp lệ, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message chứa "Có lỗi xảy ra khi tạo tài khoản"
        /// </summary>
        [Fact]
        public async Task CreateCustomerAccount_WithGenericException_ReturnsBadRequest()
        {
            // Arrange
            var model = new CreateCustomerAccountVM
            {
                username = "newcustomer",
                fullName = "New Customer",
                email = "newcustomer@example.com",
                phone = "0123456789"
            };

            _mockCustomerService
                .Setup(x => x.CreateCustomerAccountAsync(model, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateCustomerAccount(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Có lỗi xảy ra khi tạo tài khoản");
        }

        #endregion
    }
}
