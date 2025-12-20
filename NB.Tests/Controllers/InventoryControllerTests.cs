using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.WarehouseService;

namespace NB.Tests.Controllers
{
    public class InventoryControllerTests
    {
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly InventoryController _controller;

        public InventoryControllerTests()
        {
            _mockInventoryService = new Mock<IInventoryService>();
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockProductService = new Mock<IProductService>();
            _controller = new InventoryController(
                _mockInventoryService.Object,
                _mockProductService.Object,
                _mockWarehouseService.Object);
        }

        #region GetInventoryData Tests

        [Fact]
        public async Task GetInventoryData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new InventorySearch
            {
                PageIndex = 1,
                PageSize = 10,
                WarehouseId = 1,
                ProductName = "Product 1"
            };

            var inventoryList = new List<ProductInventoryDto>
            {
                new ProductInventoryDto
                {
                    ProductId = 1,
                    ProductCode = "P001",
                    ProductName = "Product 1",
                    CategoryName = "Category 1",
                    SupplierName = "Supplier 1",
                    TotalQuantity = 100,
                    WarehouseId = 1,
                    WarehouseName = "Warehouse 1",
                    LastUpdated = DateTime.Now
                }
            };

            var pagedList = new PagedList<ProductInventoryDto>(inventoryList, 1, 1, 10);

            _mockInventoryService
                .Setup(x => x.GetProductInventoryListAsync(It.IsAny<InventorySearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetInventoryData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<ProductInventoryDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(1);
            response.Data.Items.First().ProductName.Should().Be("Product 1");
        }

        [Fact]
        public async Task GetInventoryData_WithoutWarehouseId_ReturnsOkWithTotalInventory()
        {
            // Arrange
            var search = new InventorySearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            var inventoryList = new List<ProductInventoryDto>
            {
                new ProductInventoryDto
                {
                    ProductId = 1,
                    ProductCode = "P001",
                    ProductName = "Product 1",
                    TotalQuantity = 300, // Total from all warehouses
                    WarehouseId = null,
                    WarehouseName = null
                }
            };

            var pagedList = new PagedList<ProductInventoryDto>(inventoryList, 1, 1, 10);

            _mockInventoryService
                .Setup(x => x.GetProductInventoryListAsync(It.IsAny<InventorySearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetInventoryData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<ProductInventoryDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Items.First().TotalQuantity.Should().Be(300);
            response.Data.Items.First().WarehouseId.Should().BeNull();
        }

        [Fact]
        public async Task GetInventoryData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new InventorySearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockInventoryService
                .Setup(x => x.GetProductInventoryListAsync(It.IsAny<InventorySearch>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetInventoryData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<ProductInventoryDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Contain("Có lỗi xảy ra khi lấy dữ liệu tồn kho");
        }

        #endregion

        #region GetInventoryQuantity Tests

        [Fact]
        public async Task GetInventoryQuantity_WithValidData_ReturnsOkWithQuantity()
        {
            // Arrange
            var search = new ProductInventorySearch
            {
                warehouseId = 1,
                productId = 1
            };

            var warehouse = new Warehouse
            {
                WarehouseId = 1,
                WarehouseName = "Warehouse 1"
            };

            var product = new Product
            {
                ProductId = 1,
                ProductName = "Product 1"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(warehouse);

            _mockProductService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product);

            _mockInventoryService
                .Setup(x => x.GetInventoryQuantity(1, 1))
                .ReturnsAsync(150);

            // Act
            var result = await _controller.GetInventoryQuantity(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<int>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be(150);
        }

        [Fact]
        public async Task GetInventoryQuantity_WithNonExistentWarehouse_ReturnsNotFound()
        {
            // Arrange
            var search = new ProductInventorySearch
            {
                warehouseId = 999,
                productId = 1
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Warehouse?)null);

            // Act
            var result = await _controller.GetInventoryQuantity(search);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<int>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Kho không tồn tại");
        }

        [Fact]
        public async Task GetInventoryQuantity_WithNonExistentProduct_ReturnsNotFound()
        {
            // Arrange
            var search = new ProductInventorySearch
            {
                warehouseId = 1,
                productId = 999
            };

            var warehouse = new Warehouse
            {
                WarehouseId = 1,
                WarehouseName = "Warehouse 1"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(warehouse);

            _mockProductService
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _controller.GetInventoryQuantity(search);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<int>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Sản phẩm không tồn tại");
        }

        [Fact]
        public async Task GetInventoryQuantity_WithZeroQuantity_ReturnsOkWithZero()
        {
            // Arrange
            var search = new ProductInventorySearch
            {
                warehouseId = 1,
                productId = 1
            };

            var warehouse = new Warehouse
            {
                WarehouseId = 1,
                WarehouseName = "Warehouse 1"
            };

            var product = new Product
            {
                ProductId = 1,
                ProductName = "Product 1"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(warehouse);

            _mockProductService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product);

            _mockInventoryService
                .Setup(x => x.GetInventoryQuantity(1, 1))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.GetInventoryQuantity(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<int>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be(0);
        }

        [Fact]
        public async Task GetInventoryQuantity_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new ProductInventorySearch
            {
                warehouseId = 1,
                productId = 1
            };

            var warehouse = new Warehouse
            {
                WarehouseId = 1,
                WarehouseName = "Warehouse 1"
            };

            var product = new Product
            {
                ProductId = 1,
                ProductName = "Product 1"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(warehouse);

            _mockProductService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product);

            _mockInventoryService
                .Setup(x => x.GetInventoryQuantity(1, 1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetInventoryQuantity(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<int>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Contain("Có lỗi xảy ra khi lấy số lượng tồn kho");
        }

        #endregion
    }
}
