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
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionDetailService.Dto;
using NB.Service.ReturnTransactionService;
using NB.Service.ReturnTransactionService.Dto;
using NB.Service.ReturnTransactionService.ViewModels;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
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
    public class ReturnOrderControllerTests
    {
        private readonly Mock<IReturnTransactionService> _mockReturnTransactionService;
        private readonly Mock<IReturnTransactionDetailService> _mockReturnTransactionDetailService;
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly Mock<ITransactionDetailService> _mockTransactionDetailService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<ISupplierService> _mockSupplierService;
        private readonly Mock<ILogger<ReturnOrderController>> _mockLogger;
        private readonly ReturnOrderController _controller;

        // Test Data Constants
        private const int ValidUserId = 1;
        private const int ValidReturnTransactionId = 1;
        private const int ValidTransactionId = 1;
        private const int ValidPageIndex = 1;
        private const int ValidPageSize = 10;

        public ReturnOrderControllerTests()
        {
            _mockReturnTransactionService = new Mock<IReturnTransactionService>();
            _mockReturnTransactionDetailService = new Mock<IReturnTransactionDetailService>();
            _mockTransactionService = new Mock<ITransactionService>();
            _mockTransactionDetailService = new Mock<ITransactionDetailService>();
            _mockProductService = new Mock<IProductService>();
            _mockUserService = new Mock<IUserService>();
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockSupplierService = new Mock<ISupplierService>();
            _mockLogger = new Mock<ILogger<ReturnOrderController>>();

            _controller = new ReturnOrderController(
                _mockReturnTransactionService.Object,
                _mockReturnTransactionDetailService.Object,
                _mockTransactionService.Object,
                _mockTransactionDetailService.Object,
                _mockProductService.Object,
                _mockUserService.Object,
                _mockWarehouseService.Object,
                _mockSupplierService.Object,
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

        #region GetData Tests

        [Fact]
        public async Task GetData_ValidSearch_ReturnsOkWithData()
        {
            // Arrange
            var search = new ReturnOrderSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            var returnOrders = new List<ReturnOrderDto>
            {
                new ReturnOrderDto
                {
                    ReturnTransactionId = 1,
                    TransactionId = 1,
                    TransactionType = "Export",
                    Reason = "Damaged product",
                    WarehouseId = 1,
                    CustomerId = 1
                }
            };

            var pagedList = new PagedList<ReturnOrderDto>(returnOrders, ValidPageIndex, ValidPageSize, 1);

            var warehouses = new List<WarehouseDto>
            {
                new WarehouseDto { WarehouseId = 1, WarehouseName = "Warehouse A" }
            };

            _mockReturnTransactionService.Setup(x => x.GetData(search))
                .ReturnsAsync(pagedList);

            _mockWarehouseService.Setup(x => x.GetByListWarehouseId(It.IsAny<List<int>>()))
                .ReturnsAsync(warehouses);

            _mockUserService.Setup(x => x.GetAll())
                .Returns(new List<User>
                {
                    new User { UserId = 1, FullName = "Customer A", Phone = "0123456789", Email = "customer@test.com" }
                }.AsQueryable());

            // Act
            var result = await _controller.GetData(search);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<ReturnOrderDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetData_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var search = new ReturnOrderSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            var pagedList = new PagedList<ReturnOrderDto>(new List<ReturnOrderDto>(), ValidPageIndex, ValidPageSize, 0);

            _mockReturnTransactionService.Setup(x => x.GetData(search))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<ReturnOrderDto>>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetData_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var search = new ReturnOrderSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            _mockReturnTransactionService.Setup(x => x.GetData(search))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<PagedList<ReturnOrderDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region GetDetail Tests

        [Fact]
        public async Task GetDetail_InvalidId_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetDetail(0);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Id không hợp lệ");
        }

        [Fact]
        public async Task GetDetail_NegativeId_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetDetail(-1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetDetail_ReturnTransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockReturnTransactionService.Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((ReturnTransaction?)null);

            // Act
            var result = await _controller.GetDetail(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Không tìm thấy đơn trả hàng");
        }

        [Fact]
        public async Task GetDetail_OriginalTransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            var returnTransaction = new ReturnTransaction
            {
                ReturnTransactionId = ValidReturnTransactionId,
                TransactionId = ValidTransactionId,
                Reason = "Test"
            };

            _mockReturnTransactionService.Setup(x => x.GetByIdAsync(ValidReturnTransactionId))
                .ReturnsAsync(returnTransaction);

            _mockTransactionService.Setup(x => x.GetByTransactionId(ValidTransactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.GetDetail(ValidReturnTransactionId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Không tìm thấy đơn hàng gốc");
        }

        [Fact]
        public async Task GetDetail_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockReturnTransactionService.Setup(x => x.GetByIdAsync(ValidReturnTransactionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDetail(ValidReturnTransactionId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion
    }
}
