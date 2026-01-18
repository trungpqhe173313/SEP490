using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NB.API.Controllers;
using NB.Service.Dto;
using NB.Service.ProductionWeightLogService;
using NB.Service.ProductionWeightLogService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class ProductionWeightLogControllerTests
    {
        private readonly Mock<IProductionWeightLogService> _mockProductionWeightLogService;
        private readonly ProductionWeightLogController _controller;

        // Test Data Constants
        private const int ValidUserId = 1;
        private const int ValidProductionId = 1;

        public ProductionWeightLogControllerTests()
        {
            _mockProductionWeightLogService = new Mock<IProductionWeightLogService>();
            _controller = new ProductionWeightLogController(_mockProductionWeightLogService.Object);

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

        #region GetSummaryByProductionId Tests

        [Fact]
        public async Task GetSummaryByProductionId_ValidProductionId_ReturnsOkWithData()
        {
            // Arrange
            var summaryResponse = new ProductionWeightLogSummaryResponseDto
            {
                ProductionId = ValidProductionId,
                Products = new List<ProductWeightSummaryDto>
                {
                    new ProductWeightSummaryDto
                    {
                        ProductId = 1,
                        ProductName = "Product A",
                        TotalBags = 100,
                        TotalWeight = 5000m
                    },
                    new ProductWeightSummaryDto
                    {
                        ProductId = 2,
                        ProductName = "Product B",
                        TotalBags = 50,
                        TotalWeight = 2500m
                    }
                }
            };

            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Ok(summaryResponse);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as ProductionWeightLogSummaryResponseDto;
            data.Should().NotBeNull();
            data!.ProductionId.Should().Be(ValidProductionId);
            data.Products.Should().HaveCount(2);
            data.Products[0].TotalBags.Should().Be(100);
        }

        [Fact]
        public async Task GetSummaryByProductionId_EmptyProducts_ReturnsOkWithEmptyList()
        {
            // Arrange
            var summaryResponse = new ProductionWeightLogSummaryResponseDto
            {
                ProductionId = ValidProductionId,
                Products = new List<ProductWeightSummaryDto>()
            };

            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Ok(summaryResponse);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as ProductionWeightLogSummaryResponseDto;
            data.Should().NotBeNull();
            data!.Products.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSummaryByProductionId_ProductionNotFound_ReturnsStatusCode404()
        {
            // Arrange
            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Fail("Production order not found", 404);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(999))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(999);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetSummaryByProductionId_ServiceError_ReturnsStatusCode500()
        {
            // Arrange
            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Fail("Database error", 500);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetSummaryByProductionId_SingleProduct_ReturnsOkWithSingleProduct()
        {
            // Arrange
            var summaryResponse = new ProductionWeightLogSummaryResponseDto
            {
                ProductionId = ValidProductionId,
                Products = new List<ProductWeightSummaryDto>
                {
                    new ProductWeightSummaryDto
                    {
                        ProductId = 1,
                        ProductName = "Product A",
                        TotalBags = 200,
                        TotalWeight = 10000m
                    }
                }
            };

            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Ok(summaryResponse);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as ProductionWeightLogSummaryResponseDto;
            data.Should().NotBeNull();
            data!.Products.Should().HaveCount(1);
            data.Products[0].TotalWeight.Should().Be(10000m);
        }

        [Fact]
        public async Task GetSummaryByProductionId_LargeDataSet_ReturnsOkWithAllData()
        {
            // Arrange
            var products = new List<ProductWeightSummaryDto>();
            for (int i = 1; i <= 50; i++)
            {
                products.Add(new ProductWeightSummaryDto
                {
                    ProductId = i,
                    ProductName = $"Product {i}",
                    TotalBags = i * 10,
                    TotalWeight = i * 500m
                });
            }

            var summaryResponse = new ProductionWeightLogSummaryResponseDto
            {
                ProductionId = ValidProductionId,
                Products = products
            };

            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Ok(summaryResponse);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as ProductionWeightLogSummaryResponseDto;
            data.Should().NotBeNull();
            data!.Products.Should().HaveCount(50);
        }

        [Fact]
        public async Task GetSummaryByProductionId_ZeroWeights_ReturnsOkWithZeroValues()
        {
            // Arrange
            var summaryResponse = new ProductionWeightLogSummaryResponseDto
            {
                ProductionId = ValidProductionId,
                Products = new List<ProductWeightSummaryDto>
                {
                    new ProductWeightSummaryDto
                    {
                        ProductId = 1,
                        ProductName = "Product A",
                        TotalBags = 0,
                        TotalWeight = 0m
                    }
                }
            };

            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Ok(summaryResponse);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as ProductionWeightLogSummaryResponseDto;
            data!.Products[0].TotalBags.Should().Be(0);
            data.Products[0].TotalWeight.Should().Be(0m);
        }

        [Fact]
        public async Task GetSummaryByProductionId_UnauthorizedAccess_ReturnsStatusCode401()
        {
            // Arrange
            var apiResponse = ApiResponse<ProductionWeightLogSummaryResponseDto>.Fail("Unauthorized", 401);

            _mockProductionWeightLogService.Setup(x => x.GetSummaryByProductionIdAsync(ValidProductionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetSummaryByProductionId(ValidProductionId);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
        }

        #endregion
    }
}
