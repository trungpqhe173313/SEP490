using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class AnalyticsControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<AnalyticsController>> _mockLogger;
        private readonly AnalyticsController _controller;

        // Test Data Constants
        private readonly DateTime ValidFromDate = new DateTime(2024, 1, 1);
        private readonly DateTime ValidToDate = new DateTime(2024, 12, 31);
        private const int ValidUserId = 1;

        public AnalyticsControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<AnalyticsController>>();

            _controller = new AnalyticsController(
                _mockProductService.Object,
                _mockUserService.Object,
                _mockLogger.Object);

            // Setup HttpContext with user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, ValidUserId.ToString()),
                new Claim(ClaimTypes.Role, "Manager")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetTopSellingProducts Tests

        [Fact]
        public async Task GetTopSellingProducts_ValidDateRange_ReturnsOkWithData()
        {
            // Arrange
            var topProducts = new List<TopSellingProductDto>
            {
                new TopSellingProductDto
                {
                    ProductId = 1,
                    ProductName = "Product A",
                    ProductCode = "PA001",
                    TotalQuantitySold = 100,
                    TotalRevenue = 10000m,
                    NumberOfOrders = 50
                },
                new TopSellingProductDto
                {
                    ProductId = 2,
                    ProductName = "Product B",
                    ProductCode = "PB002",
                    TotalQuantitySold = 80,
                    TotalRevenue = 8000m,
                    NumberOfOrders = 40
                }
            };

            _mockProductService.Setup(x => x.GetTopSellingProducts(ValidFromDate, ValidToDate))
                .ReturnsAsync(topProducts);

            // Act
            var result = await _controller.GetTopSellingProducts(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<List<TopSellingProductDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
            apiResponse.Data![0].ProductId.Should().Be(1);
        }

        [Fact]
        public async Task GetTopSellingProducts_MissingFromDate_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetTopSellingProducts(null, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopSellingProductDto>>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Vui lòng cung cấp đầy đủ ngày bắt đầu và ngày kết thúc");
        }

        [Fact]
        public async Task GetTopSellingProducts_MissingToDate_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetTopSellingProducts(ValidFromDate, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopSellingProductDto>>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Vui lòng cung cấp đầy đủ ngày bắt đầu và ngày kết thúc");
        }

        [Fact]
        public async Task GetTopSellingProducts_FromDateGreaterThanToDate_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = new DateTime(2024, 12, 31);
            var toDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _controller.GetTopSellingProducts(fromDate, toDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopSellingProductDto>>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Ngày bắt đầu không được lớn hơn ngày kết thúc");
        }

        [Fact]
        public async Task GetTopSellingProducts_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockProductService.Setup(x => x.GetTopSellingProducts(ValidFromDate, ValidToDate))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetTopSellingProducts(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopSellingProductDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetTopSellingProducts_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            _mockProductService.Setup(x => x.GetTopSellingProducts(ValidFromDate, ValidToDate))
                .ReturnsAsync(new List<TopSellingProductDto>());

            // Act
            var result = await _controller.GetTopSellingProducts(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<List<TopSellingProductDto>>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEmpty();
        }

        #endregion

        #region GetTopCustomersByTotalSpent Tests

        [Fact]
        public async Task GetTopCustomersByTotalSpent_ValidDateRange_ReturnsOkWithData()
        {
            // Arrange
            var topCustomers = new List<TopCustomerDto>
            {
                new TopCustomerDto
                {
                    UserId = 1,
                    FullName = "Customer A",
                    Email = "customerA@test.com",
                    Phone = "0123456789",
                    TotalSpent = 50000m,
                    NumberOfOrders = 25
                },
                new TopCustomerDto
                {
                    UserId = 2,
                    FullName = "Customer B",
                    Email = "customerB@test.com",
                    Phone = "0987654321",
                    TotalSpent = 40000m,
                    NumberOfOrders = 20
                }
            };

            _mockUserService.Setup(x => x.GetTopCustomersByTotalSpent(ValidFromDate, ValidToDate))
                .ReturnsAsync(topCustomers);

            // Act
            var result = await _controller.GetTopCustomersByTotalSpent(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
            apiResponse.Data![0].UserId.Should().Be(1);
        }

        [Fact]
        public async Task GetTopCustomersByTotalSpent_MissingFromDate_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetTopCustomersByTotalSpent(null, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Vui lòng cung cấp đầy đủ ngày bắt đầu và ngày kết thúc");
        }

        [Fact]
        public async Task GetTopCustomersByTotalSpent_MissingToDate_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetTopCustomersByTotalSpent(ValidFromDate, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetTopCustomersByTotalSpent_FromDateGreaterThanToDate_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = new DateTime(2024, 12, 31);
            var toDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _controller.GetTopCustomersByTotalSpent(fromDate, toDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Ngày bắt đầu không được lớn hơn ngày kết thúc");
        }

        [Fact]
        public async Task GetTopCustomersByTotalSpent_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetTopCustomersByTotalSpent(ValidFromDate, ValidToDate))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetTopCustomersByTotalSpent(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetTopCustomersByTotalSpent_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetTopCustomersByTotalSpent(ValidFromDate, ValidToDate))
                .ReturnsAsync(new List<TopCustomerDto>());

            // Act
            var result = await _controller.GetTopCustomersByTotalSpent(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEmpty();
        }

        #endregion

        #region GetCustomerTotalSpending Tests

        [Fact]
        public async Task GetCustomerTotalSpending_ValidRequest_ReturnsOkWithData()
        {
            // Arrange
            var customerSpending = new TopCustomerDto
            {
                UserId = 1,
                FullName = "Customer A",
                Email = "customerA@test.com",
                Phone = "0123456789",
                TotalSpent = 50000m,
                NumberOfOrders = 25
            };

            var user = new NB.Model.Entities.User { UserId = 1, FullName = "Customer A" };

            _mockUserService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
            _mockUserService.Setup(x => x.GetCustomerTotalSpending(1, ValidFromDate, ValidToDate))
                .ReturnsAsync(customerSpending);

            // Act
            var result = await _controller.GetCustomerTotalSpending(1, ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<TopCustomerDto>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.UserId.Should().Be(1);
        }

        [Fact]
        public async Task GetCustomerTotalSpending_MissingFromDate_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetCustomerTotalSpending(1, null, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetCustomerTotalSpending_MissingToDate_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetCustomerTotalSpending(1, ValidFromDate, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetCustomerTotalSpending_FromDateGreaterThanToDate_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = new DateTime(2024, 12, 31);
            var toDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _controller.GetCustomerTotalSpending(1, fromDate, toDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Ngày bắt đầu không được lớn hơn ngày kết thúc");
        }

        [Fact]
        public async Task GetCustomerTotalSpending_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((NB.Model.Entities.User?)null);

            // Act
            var result = await _controller.GetCustomerTotalSpending(999, ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<UserDto>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Không tìm thấy người dùng");
        }

        [Fact]
        public async Task GetCustomerTotalSpending_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var user = new NB.Model.Entities.User { UserId = 1, FullName = "Customer A" };
            _mockUserService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
            _mockUserService.Setup(x => x.GetCustomerTotalSpending(1, ValidFromDate, ValidToDate))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCustomerTotalSpending(1, ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<TopCustomerDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion
    }
}
