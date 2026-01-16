using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.StockBatchService;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;

namespace NB.Tests.Controllers
{
    public class CustomerOrderControllerTests
    {
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly Mock<ITransactionDetailService> _mockTransactionDetailService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IStockBatchService> _mockStockBatchService;
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CustomerOrderController>> _mockLogger;
        private readonly CustomerOrderController _controller;

        // Reusable Test Data Constants
        private const int ValidCustomerId = 1;
        private const int NonExistentCustomerId = 999;
        private const int ValidTransactionId = 1;
        private const int NonExistentTransactionId = 999;
        private const int InvalidTransactionId = -1;
        private const int ZeroTransactionId = 0;
        private const int ValidWarehouseId = 1;
        private const int ValidProductId = 1;
        private const int ValidResponsibleId = 1;
        private const string ValidCustomerName = "John Doe";
        private const string ValidCustomerEmail = "john@example.com";
        private const string ValidCustomerPhone = "0123456789";
        private const string ValidWarehouseName = "Main Warehouse";
        private const string ValidProductName = "Product A";
        private const string ValidResponsibleName = "Manager";

        public CustomerOrderControllerTests()
        {
            _mockTransactionService = new Mock<ITransactionService>();
            _mockTransactionDetailService = new Mock<ITransactionDetailService>();
            _mockProductService = new Mock<IProductService>();
            _mockUserService = new Mock<IUserService>();
            _mockStockBatchService = new Mock<IStockBatchService>();
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockInventoryService = new Mock<IInventoryService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CustomerOrderController>>();
            _controller = new CustomerOrderController(
                _mockTransactionService.Object,
                _mockTransactionDetailService.Object,
                _mockProductService.Object,
                _mockStockBatchService.Object,
                _mockUserService.Object,
                _mockWarehouseService.Object,
                _mockInventoryService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        #region GetOrderList Tests

        /// <summary>
        /// TCID01: Lay danh sach don hang dang hoat dong thanh cong
        /// Input: TransactionSearch voi CustomerId = ValidCustomerId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve danh sach don hang co status draft/order/delivering
        /// </summary>
        [Fact]
        public async Task GetOrderList_WithValidCustomerId_ReturnsOkWithOrders()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = ValidCustomerId,
                PageIndex = 1,
                PageSize = 10
            };

            var user = new UserDto
            {
                UserId = ValidCustomerId,
                FullName = ValidCustomerName,
                Email = ValidCustomerEmail,
                Phone = ValidCustomerPhone
            };

            var transactions = new PagedList<TransactionDto>(
                new List<TransactionDto>
                {
                    new TransactionDto
                    {
                        TransactionId = ValidTransactionId,
                        CustomerId = ValidCustomerId,
                        WarehouseId = ValidWarehouseId,
                        Status = (int)TransactionStatus.draft,
                        TransactionDate = DateTime.Now
                    }
                }, 1, 10, 1);

            var warehouses = new List<WarehouseDto>
            {
                new WarehouseDto
                {
                    WarehouseId = ValidWarehouseId,
                    WarehouseName = ValidWarehouseName
                }
            };

            _mockUserService.Setup(x => x.GetByUserId(ValidCustomerId)).ReturnsAsync(user);
            _mockTransactionService.Setup(x => x.GetByListStatus(search, It.IsAny<List<int>>())).ReturnsAsync(transactions);
            _mockWarehouseService.Setup(x => x.GetByListWarehouseId(It.IsAny<List<int>>())).ReturnsAsync(warehouses);

            // Act
            var result = await _controller.GetOrderList(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<TransactionDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().HaveCount(1);
            response.Data.Items.First().FullName.Should().Be(ValidCustomerName);
            response.Data.Items.First().WarehouseName.Should().Be(ValidWarehouseName);
        }

        /// <summary>
        /// TCID02: Lay danh sach don hang khi CustomerId null
        /// Input: TransactionSearch voi CustomerId = null
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Yêu cầu Id khách hàng"
        /// </summary>
        [Fact]
        public async Task GetOrderList_WithNullCustomerId_ReturnsBadRequest()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = null,
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetOrderList(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Yêu cầu Id khách hàng");
        }

        /// <summary>
        /// TCID03: Lay danh sach don hang voi CustomerId khong ton tai
        /// Input: TransactionSearch voi CustomerId = NonExistentCustomerId (999)
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy người dùng"
        /// </summary>
        [Fact]
        public async Task GetOrderList_WithNonExistentCustomerId_ReturnsNotFound()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = NonExistentCustomerId,
                PageIndex = 1,
                PageSize = 10
            };

            _mockUserService.Setup(x => x.GetByUserId(NonExistentCustomerId)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetOrderList(search);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy người dùng");
        }

        /// <summary>
        /// TCID04: Lay danh sach don hang khi khong co don hang nao
        /// Input: TransactionSearch voi CustomerId = ValidCustomerId (1), khong co don hang
        /// Expected: HTTP 200 OK, Success = true, tra ve PagedList rong
        /// </summary>
        [Fact]
        public async Task GetOrderList_WithNoOrders_ReturnsEmptyList()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = ValidCustomerId,
                PageIndex = 1,
                PageSize = 10
            };

            var user = new UserDto { UserId = ValidCustomerId, FullName = ValidCustomerName };
            var emptyTransactions = new PagedList<TransactionDto>(new List<TransactionDto>(), 1, 10, 0);

            _mockUserService.Setup(x => x.GetByUserId(ValidCustomerId)).ReturnsAsync(user);
            _mockTransactionService.Setup(x => x.GetByListStatus(search, It.IsAny<List<int>>())).ReturnsAsync(emptyTransactions);

            // Act
            var result = await _controller.GetOrderList(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<TransactionDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().BeEmpty();
        }

        /// <summary>
        /// TCID05: Lay danh sach don hang khi service nem ngoai le
        /// Input: TransactionSearch voi CustomerId = ValidCustomerId (1), service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi lấy dữ liệu"
        /// </summary>
        [Fact]
        public async Task GetOrderList_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = ValidCustomerId,
                PageIndex = 1,
                PageSize = 10
            };

            var user = new UserDto { UserId = ValidCustomerId, FullName = ValidCustomerName };

            _mockUserService.Setup(x => x.GetByUserId(ValidCustomerId)).ReturnsAsync(user);
            _mockTransactionService.Setup(x => x.GetByListStatus(search, It.IsAny<List<int>>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetOrderList(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<TransactionDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy dữ liệu");
        }

        #endregion

        #region GetOrderHistory Tests

        /// <summary>
        /// TCID06: Lay lich su don hang thanh cong
        /// Input: TransactionSearch voi CustomerId = ValidCustomerId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve danh sach don hang co status done/cancel/paidInFull/partiallyPaid
        /// </summary>
        [Fact]
        public async Task GetOrderHistory_WithValidCustomerId_ReturnsOkWithHistory()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = ValidCustomerId,
                PageIndex = 1,
                PageSize = 10
            };

            var user = new UserDto
            {
                UserId = ValidCustomerId,
                FullName = ValidCustomerName
            };

            var transactions = new PagedList<TransactionDto>(
                new List<TransactionDto>
                {
                    new TransactionDto
                    {
                        TransactionId = ValidTransactionId,
                        CustomerId = ValidCustomerId,
                        WarehouseId = ValidWarehouseId,
                        Status = (int)TransactionStatus.done,
                        TransactionDate = DateTime.Now
                    }
                }, 1, 10, 1);

            var warehouses = new List<WarehouseDto>
            {
                new WarehouseDto
                {
                    WarehouseId = ValidWarehouseId,
                    WarehouseName = ValidWarehouseName
                }
            };

            _mockUserService.Setup(x => x.GetByUserId(ValidCustomerId)).ReturnsAsync(user);
            _mockTransactionService.Setup(x => x.GetByListStatus(search, It.IsAny<List<int>>())).ReturnsAsync(transactions);
            _mockWarehouseService.Setup(x => x.GetByListWarehouseId(It.IsAny<List<int>>())).ReturnsAsync(warehouses);

            // Act
            var result = await _controller.GetOrderHistory(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<TransactionDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().HaveCount(1);
        }

        /// <summary>
        /// TCID07: Lay lich su don hang khi CustomerId null
        /// Input: TransactionSearch voi CustomerId = null
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetOrderHistory_WithNullCustomerId_ReturnsBadRequest()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = null,
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetOrderHistory(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID08: Lay lich su don hang voi CustomerId khong ton tai
        /// Input: TransactionSearch voi CustomerId = NonExistentCustomerId (999)
        /// Expected: HTTP 404 NotFound, Success = false
        /// </summary>
        [Fact]
        public async Task GetOrderHistory_WithNonExistentCustomerId_ReturnsNotFound()
        {
            // Arrange
            var search = new TransactionSearch
            {
                CustomerId = NonExistentCustomerId,
                PageIndex = 1,
                PageSize = 10
            };

            _mockUserService.Setup(x => x.GetByUserId(NonExistentCustomerId)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetOrderHistory(search);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region GetDetail Tests

        /// <summary>
        /// TCID09: Lay chi tiet don hang thanh cong
        /// Input: Id = ValidTransactionId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve FullTransactionExportVM voi thong tin don hang va san pham
        /// </summary>
        [Fact]
        public async Task GetDetail_WithValidId_ReturnsOkWithDetails()
        {
            // Arrange
            var transaction = new TransactionDto
            {
                TransactionId = ValidTransactionId,
                CustomerId = ValidCustomerId,
                WarehouseId = ValidWarehouseId,
                ResponsibleId = ValidResponsibleId,
                Status = (int)TransactionStatus.order,
                TransactionDate = DateTime.Now
            };

            var customer = new NB.Model.Entities.User
            {
                UserId = ValidCustomerId,
                FullName = ValidCustomerName,
                Email = ValidCustomerEmail,
                Phone = ValidCustomerPhone
            };

            var warehouse = new WarehouseDto
            {
                WarehouseId = ValidWarehouseId,
                WarehouseName = ValidWarehouseName
            };

            var responsible = new UserDto
            {
                UserId = ValidResponsibleId,
                FullName = ValidResponsibleName
            };

            var transactionDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto
                {
                    Id = 1,
                    ProductId = ValidProductId,
                    UnitPrice = 100,
                    Quantity = 2,
                    WeightPerUnit = 1.5m
                }
            };

            var product = new ProductDto
            {
                ProductId = ValidProductId,
                ProductName = ValidProductName
            };

            _mockTransactionService.Setup(x => x.GetByTransactionId(ValidTransactionId)).ReturnsAsync(transaction);
            _mockWarehouseService.Setup(x => x.GetById(ValidWarehouseId)).ReturnsAsync(warehouse);
            _mockUserService.Setup(x => x.GetByUserId(ValidResponsibleId)).ReturnsAsync(responsible);
            _mockUserService.Setup(x => x.GetByIdAsync(ValidCustomerId)).ReturnsAsync(customer);
            _mockTransactionDetailService.Setup(x => x.GetByTransactionId(ValidTransactionId)).ReturnsAsync(transactionDetails);
            _mockProductService.Setup(x => x.GetById(ValidProductId)).ReturnsAsync(product);

            // Act
            var result = await _controller.GetDetail(ValidTransactionId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<FullTransactionExportVM>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.TransactionId.Should().Be(ValidTransactionId);
            response.Data.WarehouseName.Should().Be(ValidWarehouseName);
            response.Data.Customer.Should().NotBeNull();
            response.Data.Customer!.FullName.Should().Be(ValidCustomerName);
            response.Data.list.Should().HaveCount(1);
        }

        /// <summary>
        /// TCID10: Lay chi tiet don hang voi ID khong hop le (ID <= 0)
        /// Input: Id = InvalidTransactionId (-1)
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Id không hợp lệ"
        /// </summary>
        [Fact]
        public async Task GetDetail_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetDetail(InvalidTransactionId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<FullTransactionExportVM>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Id không hợp lệ");
        }

        /// <summary>
        /// TCID11: Lay chi tiet don hang voi ID = 0
        /// Input: Id = ZeroTransactionId (0)
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task GetDetail_WithZeroId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetDetail(ZeroTransactionId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<FullTransactionExportVM>>().Subject;
            response.Success.Should().BeFalse();
        }

        /// <summary>
        /// TCID12: Lay chi tiet don hang voi ID khong ton tai
        /// Input: Id = NonExistentTransactionId (999)
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy đơn hàng."
        /// </summary>
        [Fact]
        public async Task GetDetail_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetByTransactionId(NonExistentTransactionId)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.GetDetail(NonExistentTransactionId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<FullTransactionExportVM>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy đơn hàng.");
        }

        /// <summary>
        /// TCID13: Lay chi tiet don hang khong co san pham
        /// Input: Id = ValidTransactionId (1), transaction khong co san pham
        /// Expected: HTTP 200 OK, Success = true, tra ve FullTransactionExportVM voi list rong
        /// </summary>
        [Fact]
        public async Task GetDetail_WithNoProducts_ReturnsOkWithEmptyList()
        {
            // Arrange
            var transaction = new TransactionDto
            {
                TransactionId = ValidTransactionId,
                CustomerId = ValidCustomerId,
                WarehouseId = ValidWarehouseId,
                Status = (int)TransactionStatus.draft,
                TransactionDate = DateTime.Now
            };

            var customer = new NB.Model.Entities.User
            {
                UserId = ValidCustomerId,
                FullName = ValidCustomerName
            };

            var warehouse = new WarehouseDto
            {
                WarehouseId = ValidWarehouseId,
                WarehouseName = ValidWarehouseName
            };

            _mockTransactionService.Setup(x => x.GetByTransactionId(ValidTransactionId)).ReturnsAsync(transaction);
            _mockWarehouseService.Setup(x => x.GetById(ValidWarehouseId)).ReturnsAsync(warehouse);
            _mockUserService.Setup(x => x.GetByIdAsync(ValidCustomerId)).ReturnsAsync(customer);
            _mockTransactionDetailService.Setup(x => x.GetByTransactionId(ValidTransactionId)).ReturnsAsync(new List<TransactionDetailDto>());

            // Act
            var result = await _controller.GetDetail(ValidTransactionId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<FullTransactionExportVM>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.list.Should().BeEmpty();
        }

        /// <summary>
        /// TCID14: Lay chi tiet don hang khi service nem ngoai le
        /// Input: Id = ValidTransactionId (1), service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi lấy dữ liệu"
        /// </summary>
        [Fact]
        public async Task GetDetail_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockTransactionService.Setup(x => x.GetByTransactionId(ValidTransactionId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDetail(ValidTransactionId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<FullTransactionExportVM>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy dữ liệu");
        }

        #endregion
    }
}
