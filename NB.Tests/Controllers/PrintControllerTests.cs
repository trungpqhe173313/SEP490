using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.SupplierService;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.UserService;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class PrintControllerTests
    {
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly Mock<ITransactionDetailService> _mockTransactionDetailService;
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IStockBatchService> _mockStockBatchService;
        private readonly Mock<ISupplierService> _mockSupplierService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<ILogger<PrintController>> _mockLogger;
        private readonly PrintController _controller;

        // Test Data Constants
        private const int ValidUserId = 1;
        private const int ValidTransactionId = 1;
        private const int InvalidTransactionId = 0;

        public PrintControllerTests()
        {
            _mockTransactionService = new Mock<ITransactionService>();
            _mockTransactionDetailService = new Mock<ITransactionDetailService>();
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockProductService = new Mock<IProductService>();
            _mockStockBatchService = new Mock<IStockBatchService>();
            _mockSupplierService = new Mock<ISupplierService>();
            _mockUserService = new Mock<IUserService>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockLogger = new Mock<ILogger<PrintController>>();

            _controller = new PrintController(
                _mockTransactionService.Object,
                _mockTransactionDetailService.Object,
                _mockWarehouseService.Object,
                _mockProductService.Object,
                _mockStockBatchService.Object,
                _mockSupplierService.Object,
                _mockUserService.Object,
                _mockCloudinaryService.Object,
                _mockLogger.Object);

            // Setup HttpContext with user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, ValidUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region Print Tests

        [Fact]
        public async Task Print_InvalidTransactionId_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.Print(InvalidTransactionId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("ID giao dịch không hợp lệ");
        }

        [Fact]
        public async Task Print_NegativeTransactionId_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.Print(-1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("ID giao dịch không hợp lệ");
        }

        [Fact]
        public async Task Print_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetByTransactionId(ValidTransactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.Print(ValidTransactionId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Không tìm thấy đơn giao dịch");
        }

        [Fact]
        public async Task Print_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetByTransactionId(ValidTransactionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Print(ValidTransactionId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Có lỗi xảy ra khi tạo file in");
        }

        #endregion
    }
}
