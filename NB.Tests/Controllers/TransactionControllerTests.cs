using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class TransactionControllerTests
    {
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<ILogger<TransactionController>> _mockLogger;
        private readonly TransactionController _controller;

        // Test Data Constants
        private const int ValidUserId = 1;
        private const int ValidTransactionId = 1;
        private const int ValidPageIndex = 1;
        private const int ValidPageSize = 10;
        private readonly DateTime ValidFromDate = new DateTime(2024, 1, 1);
        private readonly DateTime ValidToDate = new DateTime(2024, 12, 31);

        public TransactionControllerTests()
        {
            _mockTransactionService = new Mock<ITransactionService>();
            _mockUserService = new Mock<IUserService>();
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockLogger = new Mock<ILogger<TransactionController>>();

            _controller = new TransactionController(
                _mockTransactionService.Object,
                _mockUserService.Object,
                _mockWarehouseService.Object,
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

        #region GetTransactionById Tests

        [Fact]
        public async Task GetTransactionById_ValidId_ReturnsOkWithData()
        {
            // Arrange
            var transactionDetail = new TransactionDetailResponseDto
            {
                TransactionId = ValidTransactionId,
                Type = "Export",
                Status = "Done",
                TransactionDate = DateTime.Now,
                TransactionCode = "TXN001",
                WarehouseId = 1,
                WarehouseName = "Warehouse A",
                CustomerId = 1,
                CustomerName = "Customer A",
                TotalCost = 10000m
            };

            _mockTransactionService.Setup(x => x.GetTransactionDetailById(ValidTransactionId))
                .ReturnsAsync(transactionDetail);

            // Act
            var result = await _controller.GetTransactionById(ValidTransactionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.TransactionId.Should().Be(ValidTransactionId);
        }

        [Fact]
        public async Task GetTransactionById_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetTransactionDetailById(999))
                .ThrowsAsync(new ArgumentException("Transaction not found"));

            // Act
            var result = await _controller.GetTransactionById(999);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetTransactionById_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetTransactionDetailById(ValidTransactionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetTransactionById(ValidTransactionId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region GetImportWeight Tests

        [Fact]
        public async Task GetImportWeight_ValidDateRange_ReturnsOkWithData()
        {
            // Arrange
            var importWeightSummary = new ImportWeightSummaryDto
            {
                FromDate = ValidFromDate,
                ToDate = ValidToDate,
                TotalWeight = 5000m,
                TransactionCount = 50,
                Details = new List<ImportWeightDetailDto>
                {
                    new ImportWeightDetailDto { SupplierId = 1, SupplierName = "Supplier A", TotalWeight = 3000m, TransactionCount = 30 },
                    new ImportWeightDetailDto { SupplierId = 2, SupplierName = "Supplier B", TotalWeight = 2000m, TransactionCount = 20 }
                }
            };

            _mockTransactionService.Setup(x => x.GetImportWeightAsync(ValidFromDate, ValidToDate))
                .ReturnsAsync(importWeightSummary);

            // Act
            var result = await _controller.GetImportWeight(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<ImportWeightSummaryDto>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.TotalWeight.Should().Be(5000m);
            apiResponse.Data.Details.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetImportWeight_FromDateGreaterThanToDate_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = new DateTime(2024, 12, 31);
            var toDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _controller.GetImportWeight(fromDate, toDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<ImportWeightSummaryDto>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("fromDate không được lớn hơn toDate");
        }

        [Fact]
        public async Task GetImportWeight_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetImportWeightAsync(ValidFromDate, ValidToDate))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetImportWeight(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<ImportWeightSummaryDto>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region GetExportWeight Tests

        [Fact]
        public async Task GetExportWeight_ValidDateRange_ReturnsOkWithData()
        {
            // Arrange
            var exportWeightSummary = new ExportWeightSummaryDto
            {
                FromDate = ValidFromDate,
                ToDate = ValidToDate,
                TotalWeight = 4000m,
                TransactionCount = 40,
                Details = new List<ExportWeightDetailDto>
                {
                    new ExportWeightDetailDto { CustomerId = 1, CustomerName = "Customer A", TotalWeight = 2500m, TransactionCount = 25 },
                    new ExportWeightDetailDto { CustomerId = 2, CustomerName = "Customer B", TotalWeight = 1500m, TransactionCount = 15 }
                }
            };

            _mockTransactionService.Setup(x => x.GetExportWeightAsync(ValidFromDate, ValidToDate))
                .ReturnsAsync(exportWeightSummary);

            // Act
            var result = await _controller.GetExportWeight(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<ExportWeightSummaryDto>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.TotalWeight.Should().Be(4000m);
            apiResponse.Data.Details.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetExportWeight_FromDateGreaterThanToDate_ReturnsBadRequest()
        {
            // Arrange
            var fromDate = new DateTime(2024, 12, 31);
            var toDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _controller.GetExportWeight(fromDate, toDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<ExportWeightSummaryDto>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("fromDate không được lớn hơn toDate");
        }

        [Fact]
        public async Task GetExportWeight_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetExportWeightAsync(ValidFromDate, ValidToDate))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetExportWeight(ValidFromDate, ValidToDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<ExportWeightSummaryDto>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region GetData Tests

        [Fact]
        public async Task GetData_ValidSearch_ReturnsOkWithData()
        {
            // Arrange
            var search = new TransactionSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            var transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    TransactionId = 1,
                    TransactionCode = "TXN001",
                    Type = "Export",
                    Status = 4,
                    WarehouseId = 1,
                    ResponsibleId = 1
                }
            };

            var pagedList = new PagedList<TransactionDto>(transactions, ValidPageIndex, ValidPageSize, 1);

            var warehouses = new List<WarehouseDto>
            {
                new WarehouseDto { WarehouseId = 1, WarehouseName = "Warehouse A" }
            };

            _mockTransactionService.Setup(x => x.GetDataForExport(search))
                .ReturnsAsync(pagedList);

            _mockWarehouseService.Setup(x => x.GetByListWarehouseId(It.IsAny<List<int>>()))
                .ReturnsAsync(warehouses);

            _mockUserService.Setup(x => x.GetQueryable())
                .Returns(new List<User>
                {
                    new User { UserId = 1, FullName = "Responsible Person", Username = "responsible" }
                }.AsQueryable());

            // Act
            var result = await _controller.GetData(search);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<TransactionDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetData_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var search = new TransactionSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            var pagedList = new PagedList<TransactionDto>(new List<TransactionDto>(), ValidPageIndex, ValidPageSize, 0);

            _mockTransactionService.Setup(x => x.GetDataForExport(search))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<TransactionDto>>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetData_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var search = new TransactionSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            _mockTransactionService.Setup(x => x.GetDataForExport(search))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<PagedList<TransactionDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region GetDetail Tests

        [Fact]
        public async Task GetDetail_ValidId_ReturnsOkWithData()
        {
            // Arrange
            var transactionDetail = new TransactionDetailResponseDto
            {
                TransactionId = ValidTransactionId,
                Type = "Export",
                Status = "Done",
                TransactionDate = DateTime.Now,
                TransactionCode = "TXN001",
                WarehouseId = 1,
                WarehouseName = "Warehouse A"
            };

            _mockTransactionService.Setup(x => x.GetTransactionDetailById(ValidTransactionId))
                .ReturnsAsync(transactionDetail);

            // Act
            var result = await _controller.GetDetail(ValidTransactionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetDetail_InvalidId_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetDetail(0);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("TransactionId không hợp lệ");
        }

        [Fact]
        public async Task GetDetail_NegativeId_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetDetail(-1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetDetail_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetTransactionDetailById(999))
                .ReturnsAsync((TransactionDetailResponseDto?)null);

            // Act
            var result = await _controller.GetDetail(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Không tìm thấy giao dịch");
        }

        [Fact]
        public async Task GetDetail_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetTransactionDetailById(ValidTransactionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDetail(ValidTransactionId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<TransactionDetailResponseDto>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region UpdateResponsible Tests

        [Fact]
        public async Task UpdateResponsible_InvalidTransactionId_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateTransactionResponsibleRequest { ResponsibleId = 1 };

            // Act
            var result = await _controller.UpdateResponsible(0, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("TransactionId không hợp lệ");
        }

        [Fact]
        public async Task UpdateResponsible_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateTransactionResponsibleRequest { ResponsibleId = 1 };

            _mockTransactionService.Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Transaction?)null);

            // Act
            var result = await _controller.UpdateResponsible(999, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Không tìm thấy giao dịch");
        }

        [Fact]
        public async Task UpdateResponsible_InvalidTransactionStatus_ReturnsBadRequest()
        {
            // Arrange
            var transaction = new Transaction
            {
                TransactionId = ValidTransactionId,
                Type = "Export",
                Status = 4, // Done status - invalid for update
                ResponsibleId = null
            };

            var request = new UpdateTransactionResponsibleRequest { ResponsibleId = 2 };

            _mockTransactionService.Setup(x => x.GetByIdAsync(ValidTransactionId))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateResponsible(ValidTransactionId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Transaction không hợp lệ");
        }

        [Fact]
        public async Task UpdateResponsible_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateTransactionResponsibleRequest { ResponsibleId = 1 };

            _mockTransactionService.Setup(x => x.GetByIdAsync(ValidTransactionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateResponsible(ValidTransactionId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion
    }
}
