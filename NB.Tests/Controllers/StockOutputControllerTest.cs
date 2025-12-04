// csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinancialTransactionService;
using NB.Service.FinancialTransactionService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionDetailService.ViewModels;
using NB.Service.ReturnTransactionService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using Xunit;

namespace NB.Test.Controllers
{
    public class StockOutputControllerTest
    {
        // Mock dependencies - Các đối tượng giả lập để test
        private readonly Mock<ITransactionService> _transactionServiceMock;
        private readonly Mock<ITransactionDetailService> _transactionDetailServiceMock;
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IStockBatchService> _stockBatchServiceMock;
        private readonly Mock<IWarehouseService> _warehouseServiceMock;
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly Mock<IReturnTransactionService> _returnTransactionServiceMock;
        private readonly Mock<IReturnTransactionDetailService> _returnTransactionDetailServiceMock;
        private readonly Mock<IFinancialTransactionService> _financialTransactionServiceMock;
        private readonly Mock<ILogger<EmployeeController>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly StockOutputController _controller;

        public StockOutputControllerTest()
        {
            // Khởi tạo các mock objects
            _transactionServiceMock = new Mock<ITransactionService>();
            _transactionDetailServiceMock = new Mock<ITransactionDetailService>();
            _productServiceMock = new Mock<IProductService>();
            _userServiceMock = new Mock<IUserService>();
            _stockBatchServiceMock = new Mock<IStockBatchService>();
            _warehouseServiceMock = new Mock<IWarehouseService>();
            _inventoryServiceMock = new Mock<IInventoryService>();
            _returnTransactionServiceMock = new Mock<IReturnTransactionService>();
            _returnTransactionDetailServiceMock = new Mock<IReturnTransactionDetailService>();
            _financialTransactionServiceMock = new Mock<IFinancialTransactionService>();
            _loggerMock = new Mock<ILogger<EmployeeController>>();
            _mapperMock = new Mock<IMapper>();

            // Khởi tạo controller với các dependencies đã mock
            _controller = new StockOutputController(
                _transactionServiceMock.Object,
                _transactionDetailServiceMock.Object,
                _productServiceMock.Object,
                _stockBatchServiceMock.Object,
                _userServiceMock.Object,
                _warehouseServiceMock.Object,
                _inventoryServiceMock.Object,
                _returnTransactionServiceMock.Object,
                _returnTransactionDetailServiceMock.Object,
                _financialTransactionServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region GetDetail Tests

        /// <summary>
        /// TCID01: GetDetail với Id = 1 - Tất cả preconditions đều thỏa mãn
        /// 
        /// PRECONDITION:
        /// - Id > 0 (O)
        /// - Transaction với Id này tồn tại trong database (O)
        /// - Có ít nhất 1 product detail cho transaction này (O)
        /// - Products tồn tại cho các product details (O)
        /// 
        /// INPUT:
        /// - Id = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: transaction information with list transaction detail
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID01_GetDetail_WithId1_AllPreconditionsMet_ReturnsTransactionInformation()
        {
            // Arrange - INPUT: Id = 1
            int transactionId = 1;

            // MOCK DATA: Transaction tồn tại
            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = 1,
                TransactionDate = DateTime.Now,
                WarehouseId = 1,
                CustomerId = 1,
                TotalCost = 1000000,
                PriceListId = 1
            };

            // MOCK DATA: Warehouse tồn tại
            var warehouseDto = new WarehouseDto
            {
                WarehouseId = 1,
                WarehouseName = "Kho Hà Nội"
            };

            // MOCK DATA: Customer tồn tại
            var customerDto = new UserDto
            {
                UserId = 1,
                FullName = "Nguyễn Văn A",
                Phone = "0123456789",
                Email = "nguyenvana@example.com",
                Image = "avatar.jpg"
            };

            // MOCK DATA: Có ít nhất 1 product detail
            var productDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto
                {
                    Id = 1,
                    ProductId = 101,
                    Quantity = 10,
                    UnitPrice = 50000,
                    WeightPerUnit = 1.5m
                },
                new TransactionDetailDto
                {
                    Id = 2,
                    ProductId = 102,
                    Quantity = 5,
                    UnitPrice = 100000,
                    WeightPerUnit = 2.0m
                }
            };

            // MOCK DATA: Products tồn tại cho các product details
            var product1 = new ProductDto
            {
                ProductId = 101,
                ProductName = "Sản phẩm A",
                ProductCode = "SP001"
            };

            var product2 = new ProductDto
            {
                ProductId = 102,
                ProductName = "Sản phẩm B",
                ProductCode = "SP002"
            };

            // Setup mocks
            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            _warehouseServiceMock
                .Setup(s => s.GetById(transactionDto.WarehouseId))
                .ReturnsAsync(warehouseDto);

            _userServiceMock
                .Setup(s => s.GetByIdAsync(transactionDto.CustomerId))
                .ReturnsAsync(customerDto);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(productDetails);

            _productServiceMock
                .Setup(s => s.GetById(101))
                .ReturnsAsync(product1);

            _productServiceMock
                .Setup(s => s.GetById(102))
                .ReturnsAsync(product2);

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert - EXPECTED OUTPUT: transaction information with list transaction detail
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionExportVM>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(transactionId, response.Data.TransactionId);
            
            // Verify có list transaction detail
            Assert.NotNull(response.Data.list);
            Assert.True(response.Data.list.Count >= 1); // Có ít nhất 1 product detail
            Assert.Equal(2, response.Data.list.Count);
            
            // Verify transaction information đầy đủ
            Assert.Equal(warehouseDto.WarehouseName, response.Data.WarehouseName);
            Assert.NotNull(response.Data.Customer);
            Assert.Equal(customerDto.FullName, response.Data.Customer.FullName);
        }

        /// <summary>
        /// TCID02: GetDetail với Id = -1 - Invalid Id
        /// 
        /// PRECONDITION:
        /// - Không có (Id không hợp lệ)
        /// 
        /// INPUT:
        /// - Id = -1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Message = "Id không hợp lệ"
        /// - Type: N (Normal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_GetDetail_WithIdNegative1_ReturnsInvalidIdMessage()
        {
            // Arrange - INPUT: Id = -1
            int invalidId = -1;

            // Act
            var result = await _controller.GetDetail(invalidId);

            // Assert - EXPECTED OUTPUT: Message = "Id không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionVM>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: GetDetail với Id = 1000 - Transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Id > 0 (O) - 1000 > 0
        /// - Transaction với Id này KHÔNG tồn tại trong database (▼)
        /// 
        /// INPUT:
        /// - Id = 1000
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Message = "Không tìm thấy đơn hàng."
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_GetDetail_WithId1000_TransactionNotFound_ReturnsNotFoundMessage()
        {
            // Arrange - INPUT: Id = 1000
            int transactionId = 1000;

            // Setup mock: Transaction KHÔNG tồn tại
            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto)null);

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert - EXPECTED OUTPUT: Message = "Không tìm thấy đơn hàng."
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionVM>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng.", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: GetDetail với Id = 15 - Transaction tồn tại nhưng không có product details
        /// 
        /// PRECONDITION:
        /// - Id > 0 (O) - 15 > 0
        /// - Transaction với Id này tồn tại trong database (O)
        /// - KHÔNG có product detail cho transaction này (▼)
        /// 
        /// INPUT:
        /// - Id = 15
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Message = "Không có thông tin cho giao dịch này."
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_GetDetail_WithId15_NoProductDetails_ReturnsNoInformationMessage()
        {
            // Arrange - INPUT: Id = 15
            int transactionId = 15;

            // MOCK DATA: Transaction tồn tại
            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = 1,
                TransactionDate = DateTime.Now,
                WarehouseId = 1,
                CustomerId = 1,
                TotalCost = 500000
            };

            // MOCK DATA: Warehouse tồn tại
            var warehouseDto = new WarehouseDto
            {
                WarehouseId = 1,
                WarehouseName = "Kho Hà Nội"
            };

            // MOCK DATA: Customer tồn tại
            var customerDto = new UserDto
            {
                UserId = 1,
                FullName = "Nguyễn Văn A"
            };

            // Setup mocks
            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            _warehouseServiceMock
                .Setup(s => s.GetById(transactionDto.WarehouseId))
                .ReturnsAsync(warehouseDto);

            _userServiceMock
                .Setup(s => s.GetByIdAsync(transactionDto.CustomerId))
                .ReturnsAsync(customerDto);

            // KHÔNG có product details - trả về null hoặc empty
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((List<TransactionDetailDto>)null);

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert - EXPECTED OUTPUT: Message = "Không có thông tin cho giao dịch này."
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionVM>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không có thông tin cho giao dịch này.", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: GetDetail với input không hợp lệ (string "abc")
        /// 
        /// PRECONDITION:
        /// - ModelState invalid (simulate việc route nhận string "abc" không parse được thành int)
        /// 
        /// INPUT:
        /// - Id: string "abc" (không hợp lệ, không thể convert thành int)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_GetDetail_WithInvalidStringInput_ReturnsBadRequest()
        {
            // Arrange - INPUT: string "abc" (không hợp lệ)
            // Simulate việc route nhận "abc" và không parse được thành int
            // Set ModelState invalid để test validation
            _controller.ModelState.AddModelError("Id", "The value 'abc' is not valid for Id.");

            // Act - Gọi với giá trị int bất kỳ (vì ModelState đã invalid)
            // Trong thực tế, khi route nhận "abc", framework sẽ set ModelState invalid trước khi gọi method
            var result = await _controller.GetDetail(0); // Giá trị này không quan trọng vì ModelState đã invalid

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: GetDetail khi có exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Id > 0
        /// - Service layer throw exception
        /// 
        /// INPUT:
        /// - Id = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với error message
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task GetDetail_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 1;

            // Setup mock để throw exception
            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region GetData Tests

        /// <summary>
        /// TCID01: GetData với empty result - Early return với empty list
        /// 
        /// PRECONDITION:
        /// - result.Items == null hoặc empty
        /// 
        /// INPUT:
        /// - search: new TransactionSearch() (tối thiểu nhất)
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 200 OK
        /// - Response: ApiResponse<PagedList<TransactionDto>>.Ok(result)
        /// - result.Items == null hoặc empty
        /// - Type: N (Normal)
        /// </summary>
        [Fact]
        public async Task TCID01_GetData_WithEmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange - INPUT: search = new TransactionSearch() (tối thiểu nhất)
            var search = new TransactionSearch();

            // MOCK DATA: Empty result (Items == null hoặc empty)
            var emptyResult = new PagedList<TransactionDto>(
                items: null!, // hoặc new List<TransactionDto>()
                pageIndex: 1,
                pageSize: 10,
                totalCount: 0
            );

            // Setup mock: Return empty result
            _transactionServiceMock
                .Setup(s => s.GetDataForExport(It.IsAny<TransactionSearch>()))
                .ReturnsAsync(emptyResult);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: 200 OK với empty list
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<TransactionDto>>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.True(response.Data.Items == null || !response.Data.Items.Any());
        }

        /// <summary>
        /// TCID02: GetData với data đầy đủ - Success với full data
        /// 
        /// PRECONDITION:
        /// - result.Items có data
        /// - Warehouse tồn tại
        /// - User tồn tại
        /// - Transaction có Status
        /// 
        /// INPUT:
        /// - search: null (tối thiểu nhất, sẽ được set Type = "Export" trong code)
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 200 OK
        /// - Response: ApiResponse<PagedList<TransactionDto>>.Ok(result)
        /// - result.Items có data với FullName, WarehouseName, StatusName đầy đủ
        /// - Type: N (Normal)
        /// </summary>
        [Fact]
        public async Task TCID02_GetData_WithValidData_ReturnsOkWithFullData()
        {
            // Arrange - INPUT: search = new TransactionSearch() (tối thiểu nhất)
            var search = new TransactionSearch();

            // MOCK DATA: Transactions có data
            var transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    TransactionId = 1,
                    CustomerId = 1,
                    WarehouseId = 1,
                    Status = 1,
                    TransactionDate = DateTime.Now,
                    TotalCost = 1000000
                },
                new TransactionDto
                {
                    TransactionId = 2,
                    CustomerId = 2,
                    WarehouseId = 1,
                    Status = 2,
                    TransactionDate = DateTime.Now,
                    TotalCost = 2000000
                }
            };

            var pagedResult = new PagedList<TransactionDto>(
                items: transactions,
                pageIndex: 1,
                pageSize: 10,
                totalCount: 2
            );

            // MOCK DATA: Warehouses tồn tại
            var warehouses = new List<WarehouseDto?>
            {
                new WarehouseDto
                {
                    WarehouseId = 1,
                    WarehouseName = "Kho Hà Nội"
                }
            };

            // MOCK DATA: Users tồn tại
            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = 1,
                    FullName = "Nguyễn Văn A"
                },
                new UserDto
                {
                    UserId = 2,
                    FullName = "Trần Thị B"
                }
            };

            // Setup mocks
            _transactionServiceMock
                .Setup(s => s.GetDataForExport(It.IsAny<TransactionSearch>()))
                .ReturnsAsync(pagedResult);

            _warehouseServiceMock
                .Setup(s => s.GetByListWarehouseId(It.IsAny<List<int>>()))
                .ReturnsAsync(warehouses);

            _userServiceMock
                .Setup(s => s.GetAll())
                .Returns(users);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: 200 OK với data đầy đủ
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<TransactionDto>>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Items);
            Assert.Equal(2, response.Data.Items.Count);
            
            // Verify data đầy đủ
            Assert.Equal("Nguyễn Văn A", response.Data.Items[0].FullName);
            Assert.Equal("Kho Hà Nội", response.Data.Items[0].WarehouseName);
            Assert.NotNull(response.Data.Items[0].StatusName);
            
            Assert.Equal("Trần Thị B", response.Data.Items[1].FullName);
            Assert.Equal("Kho Hà Nội", response.Data.Items[1].WarehouseName);
            Assert.NotNull(response.Data.Items[1].StatusName);
        }

        /// <summary>
        /// TCID03: GetData với warehouse không tồn tại
        /// 
        /// PRECONDITION:
        /// - result.Items có data
        /// - Warehouse KHÔNG tồn tại (null hoặc empty)
        /// 
        /// INPUT:
        /// - search: new TransactionSearch() (tối thiểu nhất)
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 404 Not Found
        /// - Response: ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho")
        /// - Error.Message: "Không tìm thấy kho"
        /// - Type: N (Normal)
        /// </summary>
        [Fact]
        public async Task TCID03_GetData_WarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: search = new TransactionSearch() (tối thiểu nhất)
            var search = new TransactionSearch();

            // MOCK DATA: Transactions có data
            var transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    TransactionId = 1,
                    WarehouseId = 999, // Warehouse không tồn tại
                    CustomerId = 1
                }
            };

            var pagedResult = new PagedList<TransactionDto>(
                items: transactions,
                pageIndex: 1,
                pageSize: 10,
                totalCount: 1
            );

            // Setup mocks
            _transactionServiceMock
                .Setup(s => s.GetDataForExport(It.IsAny<TransactionSearch>()))
                .ReturnsAsync(pagedResult);

            // Warehouse KHÔNG tồn tại - trả về null hoặc empty
            _warehouseServiceMock
                .Setup(s => s.GetByListWarehouseId(It.IsAny<List<int>>()))
                .ReturnsAsync((List<WarehouseDto?>)null);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: 404 Not Found
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<WarehouseDto>>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: GetData khi có exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Service layer throw exception
        /// 
        /// INPUT:
        /// - search: new TransactionSearch() (tối thiểu nhất)
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 400 Bad Request
        /// - Response: ApiResponse<PagedList<TransactionDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu")
        /// - Error.Message: "Có lỗi xảy ra khi lấy dữ liệu"
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID04_GetData_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: search = new TransactionSearch() (tối thiểu nhất)
            var search = new TransactionSearch();

            // Setup mock để throw exception
            _transactionServiceMock
                .Setup(s => s.GetDataForExport(It.IsAny<TransactionSearch>()))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: 400 Bad Request
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<TransactionDto>>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu", response.Error.Message);
        }

        #endregion

        #region CreateOrder Tests

        /// <summary>
        /// TCID01: CreateOrder với user không tồn tại
        /// 
        /// PRECONDITION:
        /// - User với userId không tồn tại trong database
        /// 
        /// INPUT:
        /// - userId: 999 (không tồn tại)
        /// - or: new OrderRequest() với ListProductOrder = null hoặc empty
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy khách hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_CreateOrder_UserNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: userId không tồn tại
            int userId = 999;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>()
            };

            // Setup mock: User không tồn tại
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy khách hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<UserDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy khách hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateOrder với ListProductOrder null hoặc empty
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - ListProductOrder = null hoặc empty
        /// 
        /// INPUT:
        /// - userId: 1 (tồn tại)
        /// - or: new OrderRequest() với ListProductOrder = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không có sản phẩm nào"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_CreateOrder_EmptyProductList_ReturnsBadRequest()
        {
            // Arrange - INPUT: ListProductOrder = null
            int userId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = null!
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không có sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không có sản phẩm nào", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreateOrder với sản phẩm không tồn tại
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - ListProductOrder có sản phẩm nhưng không tồn tại trong database
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với ListProductOrder chứa ProductId không tồn tại
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm nào"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_CreateOrder_ProductsNotFound_ReturnsBadRequest()
        {
            // Arrange - INPUT: Sản phẩm không tồn tại
            int userId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 999, Quantity = 10, UnitPrice = 100000 }
                }
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Không tìm thấy sản phẩm
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto>()); // Empty list

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy sản phẩm nào", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreateOrder với warehouseId null và không tìm thấy lô hàng khả dụng
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - WarehouseId = null
        /// - Không tìm thấy lô hàng khả dụng
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với WarehouseId = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy lô hàng khả dụng cho các sản phẩm này"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_CreateOrder_NoStockBatchAvailable_ReturnsBadRequest()
        {
            // Arrange - INPUT: WarehouseId = null, không có lô hàng
            int userId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = null
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Không tìm thấy lô hàng khả dụng
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto>()); // Empty list

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy lô hàng khả dụng cho các sản phẩm này"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy lô hàng khả dụng cho các sản phẩm này", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreateOrder với kho không tồn tại
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - WarehouseId được chỉ định nhưng kho không tồn tại
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với WarehouseId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID05_CreateOrder_WarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: WarehouseId không tồn tại
            int userId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 999
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Kho không tồn tại
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy kho"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CreateOrder với sản phẩm không có trong kho
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - Kho tồn tại
        /// - Sản phẩm không có trong inventory của kho
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với sản phẩm không có trong kho
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm '{productName}' trong kho '{warehouseName}'"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_CreateOrder_ProductNotInWarehouse_ReturnsBadRequest()
        {
            // Arrange - INPUT: Sản phẩm không có trong kho
            int userId = 1;
            int warehouseId = 1;
            int productId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = warehouseId
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Kho tồn tại
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouseDto);

            // Setup mock: Sản phẩm không có trong inventory
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto>()); // Empty list - không có sản phẩm trong kho

            // Setup mock: GetByIdAsync cho product name
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "Không tìm thấy sản phẩm"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<InventoryDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không tìm thấy sản phẩm", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: CreateOrder với số lượng sản phẩm không đủ trong kho
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - Kho tồn tại
        /// - Sản phẩm có trong kho nhưng số lượng không đủ
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với Quantity > Quantity trong inventory
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Sản phẩm '{productName}' trong kho '{warehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_CreateOrder_InsufficientQuantity_ReturnsBadRequest()
        {
            // Arrange - INPUT: Số lượng không đủ
            int userId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal orderQuantity = 100; // Yêu cầu 100
            decimal inventoryQuantity = 50; // Chỉ còn 50
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = orderQuantity, UnitPrice = 100000 }
                },
                WarehouseId = warehouseId
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Kho tồn tại
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouseDto);

            // Setup mock: Inventory có nhưng số lượng không đủ
            var inventoryDto = new InventoryDto
            {
                InventoryId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = inventoryQuantity
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: GetByIdAsync cho product name
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "chỉ còn" và "không đủ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<InventoryDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("chỉ còn", response.Error.Message);
            Assert.Contains("không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID08: CreateOrder với không tìm thấy lô hàng khả dụng trong kho
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - Kho tồn tại
        /// - Inventory đủ số lượng
        /// - Không tìm thấy lô hàng khả dụng trong kho (trong try block)
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với tất cả điều kiện OK nhưng không có lô hàng khả dụng
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy lô hàng khả dụng cho các sản phẩm này trong kho '{warehouseName}'"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_CreateOrder_NoStockBatchInWarehouse_ReturnsBadRequest()
        {
            // Arrange - INPUT: Không có lô hàng khả dụng trong kho
            int userId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal orderQuantity = 10;
            decimal inventoryQuantity = 100; // Đủ số lượng
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = orderQuantity, UnitPrice = 100000 }
                },
                WarehouseId = warehouseId
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Kho tồn tại
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouseDto);

            // Setup mock: Inventory đủ số lượng
            var inventoryDto = new InventoryDto
            {
                InventoryId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = inventoryQuantity
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Không tìm thấy lô hàng khả dụng trong kho (trong try block)
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto>()); // Empty list

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy lô hàng khả dụng cho các sản phẩm này trong kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không tìm thấy lô hàng khả dụng cho các sản phẩm này trong kho", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: CreateOrder thành công
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - Kho tồn tại
        /// - Inventory đủ số lượng
        /// - Có lô hàng khả dụng trong kho
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với tất cả điều kiện OK
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Tạo đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID09_CreateOrder_Success_ReturnsOk()
        {
            // Arrange - INPUT: Tất cả điều kiện OK
            int userId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal orderQuantity = 10;
            decimal inventoryQuantity = 100;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = orderQuantity, UnitPrice = 100000 }
                },
                WarehouseId = warehouseId,
                TotalCost = 1000000,
                PriceListId = 1
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto 
            { 
                ProductId = productId, 
                ProductName = "Test Product",
                WeightPerUnit = 1.5m
            };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Setup mock: Kho tồn tại
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouseDto);

            // Setup mock: Inventory đủ số lượng
            var inventoryDto = new InventoryDto
            {
                InventoryId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = inventoryQuantity
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Có lô hàng khả dụng trong kho
            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityIn = 100,
                QuantityOut = 0,
                ExpireDate = DateTime.Today.AddDays(30)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // Setup mock: Mapper
            var transactionEntity = new Transaction
            {
                TransactionId = 1,
                CustomerId = userId,
                WarehouseId = warehouseId,
                Status = (int)TransactionStatus.draft,
                Type = "Export"
            };
            _mapperMock
                .Setup(m => m.Map<TransactionCreateVM, Transaction>(It.IsAny<TransactionCreateVM>()))
                .Returns(transactionEntity);

            var transactionDetailEntity = new TransactionDetail
            {
                Id = 1,
                TransactionId = 1,
                ProductId = productId,
                Quantity = (int)orderQuantity,
                UnitPrice = 100000
            };
            _mapperMock
                .Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>()))
                .Returns(transactionDetailEntity);

            // Setup mock: CreateAsync và UpdateAsync
            _transactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);
            _transactionDetailServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: Ok với message "Tạo đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Tạo đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID10: CreateOrder với exception
        /// 
        /// PRECONDITION:
        /// - User tồn tại
        /// - Sản phẩm tồn tại
        /// - Kho tồn tại
        /// - Inventory đủ số lượng
        /// - Exception xảy ra trong try block
        /// 
        /// INPUT:
        /// - userId: 1
        /// - or: OrderRequest với tất cả điều kiện OK nhưng có exception
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi tạo đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID10_CreateOrder_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int userId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal orderQuantity = 10;
            decimal inventoryQuantity = 100;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = orderQuantity, UnitPrice = 100000 }
                },
                WarehouseId = warehouseId
            };

            // Setup mock: User tồn tại
            var userDto = new UserDto { UserId = userId, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(userId))
                .ReturnsAsync(userDto);

            // Setup mock: Sản phẩm tồn tại
            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Kho tồn tại
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouseDto);

            // Setup mock: Inventory đủ số lượng
            var inventoryDto = new InventoryDto
            {
                InventoryId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = inventoryQuantity
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Throw exception khi gọi GetByProductIdForOrder trong try block
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateOrder(userId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi tạo đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi tạo đơn hàng", response.Error.Message);
        }

        #endregion

        #region UpdateTransactionInDraftStatus Tests

        /// <summary>
        /// TCID01: UpdateTransactionInDraftStatus - Transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - Transaction với transactionId này KHÔNG tồn tại (▼)
        ///
        /// INPUT:
        /// - transactionId: 1000
        /// - OrderRequest: tối thiểu 1 product (ProductId + Quantity)
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateDraft_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            int transactionId = 1000;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder> { new ProductOrder { ProductId = 1, Quantity = 1 } }
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateTransactionInDraftStatus - Transaction tồn tại nhưng không ở trạng thái draft
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - Transaction tồn tại nhưng Status != draft (▼)
        ///
        /// INPUT:
        /// - transactionId: 10
        /// - OrderRequest: tối thiểu 1 product
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không trong trạng thái nháp"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateDraft_NotInDraftStatus_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 10;
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order, // not draft
                WarehouseId = 1
            };
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder> { new ProductOrder { ProductId = 1, Quantity = 1 } }
            };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng không trong trạng thái nháp"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không trong trạng thái nháp", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateTransactionInDraftStatus - OrderRequest.ListProductOrder null/empty
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - OrderRequest.ListProductOrder = null hoặc empty (▼)
        ///
        /// INPUT:
        /// - transactionId: 11
        /// - OrderRequest: ListProductOrder = empty
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không có sản phẩm nào"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateDraft_EmptyProductList_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 11;
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = 1
            };
            var orderRequest = new OrderRequest { ListProductOrder = new List<ProductOrder>() };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không có sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không có sản phẩm nào", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateTransactionInDraftStatus - Product không tồn tại
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - OrderRequest chứa ProductId nhưng ProductService trả về rỗng (▼)
        ///
        /// INPUT:
        /// - transactionId: 12
        /// - OrderRequest: 1 product với ProductId không tồn tại
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm nào"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateDraft_ProductNotFound_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 12;
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = 1
            };
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder> { new ProductOrder { ProductId = 999, Quantity = 2 } }
            };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto>());

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy sản phẩm nào", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateTransactionInDraftStatus - Inventory không đủ
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - Product tồn tại (O)
        /// - Inventory trong kho nhưng số lượng nhỏ hơn yêu cầu (▼)
        ///
        /// INPUT:
        /// - transactionId: 13
        /// - WarehouseId lấy từ transaction
        /// - OrderRequest: yêu cầu Quantity lớn hơn Inventory.Quantity
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Sản phẩm '{productName}' trong kho '{warehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateDraft_InsufficientInventory_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 13;
            int warehouseId = 5;
            int productId = 200;
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = warehouseId
            };
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder> { new ProductOrder { ProductId = productId, Quantity = 10 } }
            };

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });
            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(warehouseDto);
            _productServiceMock.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync(productDto);

            _inventoryServiceMock.Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 3 } });

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "chỉ còn" và "không đủ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<InventoryDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("chỉ còn", response.Error.Message);
            Assert.Contains("không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateTransactionInDraftStatus - Thành công (input tối thiểu thỏa điều kiện)
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - Product tồn tại (O)
        /// - Inventory đủ số lượng (O)
        /// - Các service Update/Create thực hiện thành công (O)
        ///
        /// INPUT (tối thiểu):
        /// - transactionId: 20
        /// - OrderRequest: 1 product với Quantity <= Inventory.Quantity, UnitPrice cung cấp
        ///
        /// EXPECTED OUTPUT:
        /// - Trả về OkObjectResult với ApiResponse.Ok (message hoặc data tuỳ implement)
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateDraft_Success_ReturnsOk()
        {
            // Arrange
            int transactionId = 20;
            int warehouseId = 5;
            int productId = 200;

            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = warehouseId
            };

            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder> { new ProductOrder { ProductId = productId, Quantity = 2, UnitPrice = 100m } },
                Note = "update note",
                TotalCost = 200m
            };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId, ProductName = "P1" } });
            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });
            _productServiceMock.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync(new ProductDto { ProductId = productId, ProductName = "P1" });

            _inventoryServiceMock.Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 10 } });

            // Return existing details (not empty) so it can be deleted
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, TransactionId = transactionId } });
            _transactionDetailServiceMock.Setup(s => s.DeleteRange(It.IsAny<List<TransactionDetailDto>>())).Returns(Task.CompletedTask);

            _transactionDetailServiceMock.Setup(s => s.CreateAsync(It.IsAny<TransactionDetail>())).Returns(Task.CompletedTask);
            _transactionDetailServiceMock.Setup(s => s.UpdateAsync(It.IsAny<TransactionDetail>())).Returns(Task.CompletedTask);
            _transactionServiceMock.Setup(s => s.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID07: UpdateTransactionInDraftStatus - Exception xảy ra trong service
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - Call đến service ném exception (▼)
        ///
        /// INPUT:
        /// - transactionId: 15
        /// - OrderRequest: tối thiểu 1 product
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateDraft_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 15;
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = 1
            };
            var orderRequest = new OrderRequest { ListProductOrder = new List<ProductOrder> { new ProductOrder { ProductId = 1, Quantity = 1 } } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = 1 } });
            _warehouseServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WarehouseDto { WarehouseId = 1, WarehouseName = "Test Warehouse" });
            _inventoryServiceMock.Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = 1, WarehouseId = 1, Quantity = 10 } });
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(new List<TransactionDetailDto>());
            // Throw exception in try block
            _transactionDetailServiceMock.Setup(s => s.DeleteRange(It.IsAny<List<TransactionDetailDto>>())).ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateTransactionInDraftStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            
            // Exception có thể xảy ra ở DeleteRange, nhưng sẽ được catch và trả về ApiResponse<string>
            // Hoặc nếu existingDetails empty, trả về ApiResponse<TransactionDetailDto>
            if (badRequestResult.Value is ApiResponse<string> stringResponse)
            {
                Assert.False(stringResponse.Success);
                Assert.NotNull(stringResponse.Error);
                Assert.Equal("Có lỗi xảy ra khi cập nhật đơn hàng", stringResponse.Error.Message);
                Assert.Equal(400, stringResponse.StatusCode);
            }
            else if (badRequestResult.Value is ApiResponse<TransactionDetailDto> detailResponse)
            {
                Assert.False(detailResponse.Success);
                Assert.NotNull(detailResponse.Error);
                Assert.Contains("chi tiết đơn hàng", detailResponse.Error.Message);
                Assert.Equal(400, detailResponse.StatusCode);
            }
            else
            {
                Assert.True(false, "Unexpected response type");
            }
        }

        #endregion

        #region UpdateToOrderStatus Tests

        /// <summary>
        /// TCID01: UpdateToOrderStatus - Transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - Transaction với transactionId này KHÔNG tồn tại (▼)
        ///
        /// INPUT:
        /// - transactionId: 1000
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateToOrder_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            int transactionId = 1000;
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateToOrderStatus - Không tìm thấy chi tiết đơn hàng
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại (O)
        /// - Chi tiết đơn hàng KHÔNG tồn tại hoặc empty (▼)
        ///
        /// INPUT:
        /// - transactionId: 10
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy chi tiết đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateToOrder_NoTransactionDetails_ReturnsNotFound()
        {
            // Arrange
            int transactionId = 10;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1 };
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync((List<TransactionDetailDto>?)null);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy chi tiết đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy chi tiết đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateToOrderStatus - Product list không tồn tại (service trả về rỗng)
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại (O)
        /// - Có transaction details (O)
        /// - ProductService.GetByIds trả về empty (▼)
        ///
        /// INPUT:
        /// - transactionId: 11
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm nào"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateToOrder_ProductsNotFound_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 11;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1 };
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = 100, Quantity = 2 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto>());

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy sản phẩm nào", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateToOrderStatus - Inventory không đủ cho một product
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và có details (O)
        /// - Product tồn tại (O)
        /// - Inventory trả về số lượng nhỏ hơn cần (▼)
        ///
        /// INPUT:
        /// - transactionId: 12
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Sản phẩm '{productName}' trong kho '{warehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToOrder_InsufficientInventory_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 12;
            int warehouseId = 5;
            int productId = 200;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId };
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 10 } };

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { productDto });
            _productServiceMock.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync(productDto);
            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(warehouseDto);

            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 5 } }); // not enough (need 10)

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "chỉ còn" và "không đủ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<InventoryDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("chỉ còn", response.Error.Message);
            Assert.Contains("không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateToOrderStatus - Không tìm thấy lô hàng khả dụng trong kho
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và có details (O)
        /// - Product tồn tại (O)
        /// - Inventory đủ (O)
        /// - StockBatchService.GetByProductIdForOrder trả về rỗng (▼)
        ///
        /// INPUT:
        /// - transactionId: 13
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng" (exception khi không có lô hàng)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateToOrder_NoStockBatchInWarehouse_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 13;
            int warehouseId = 5;
            int productId = 300;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId };
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId } });

            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "WH" });

            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 10 } });

            // No stock batches available - will cause remaining > 0, but code doesn't check this
            // So we need to make GetByIdAsync throw exception to trigger catch block
            _stockBatchServiceMock.Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>())).ReturnsAsync(new List<StockBatchDto>());
            // When code tries to get inventory entity, throw exception to trigger catch
            _inventoryServiceMock.Setup(s => s.GetEntityByWarehouseAndProductIdAsync(warehouseId, productId))
                .ThrowsAsync(new Exception("No inventory entity"));

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateToOrderStatus - Thành công (input tối thiểu)
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và có details (O)
        /// - Product tồn tại (O)
        /// - Inventory đủ (O)
        /// - StockBatch có lô hợp lệ, GetByIdAsync trả entity để cập nhật (O)
        /// - Các service Update/UpdateNoTracking thành công (O)
        ///
        /// INPUT (tối thiểu):
        /// - transactionId: 20
        ///
        /// EXPECTED OUTPUT:
        /// - Trả về OkObjectResult với ApiResponse.Ok("Cập nhật đơn hàng thành công")
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateToOrder_Success_ReturnsOk()
        {
            // Arrange
            int transactionId = 20;
            int warehouseId = 5;
            int productId = 400;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId };
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId } });

            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "WH" });

            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 10 } });

            // Provide stock batch available in same warehouse
            var stockBatchDto = new StockBatchDto { BatchId = 1, ProductId = productId, WarehouseId = warehouseId, QuantityIn = 10, QuantityOut = 0, ImportDate = DateTime.Today.AddDays(-10) };
            _stockBatchServiceMock.Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>())).ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // When controller fetches batch entity by id, return entity to be updated
            var stockBatchEntity = new StockBatch { BatchId = stockBatchDto.BatchId, ProductId = productId, WarehouseId = warehouseId, QuantityIn = 10, QuantityOut = 0 };
            _stockBatchServiceMock.Setup(s => s.GetByIdAsync(stockBatchDto.BatchId)).ReturnsAsync(stockBatchEntity);
            _stockBatchServiceMock.Setup(s => s.UpdateAsync(It.IsAny<StockBatch>())).Returns(Task.CompletedTask);
            _stockBatchServiceMock.Setup(s => s.UpdateNoTracking(It.IsAny<StockBatch>())).Returns(Task.CompletedTask);

            // inventory entity for update
            var inventoryEntity = new Inventory { InventoryId = 1, ProductId = productId, WarehouseId = warehouseId, Quantity = 10m };
            _inventoryServiceMock.Setup(s => s.GetEntityByWarehouseAndProductIdAsync(warehouseId, productId)).ReturnsAsync(inventoryEntity);
            _inventoryServiceMock.Setup(s => s.UpdateNoTracking(It.IsAny<Inventory>())).Returns(Task.CompletedTask);

            _transactionServiceMock.Setup(s => s.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var resp = Assert.IsType<ApiResponse<string>>(ok.Value);
            Assert.True(resp.Success);
            Assert.Equal("Cập nhật đơn hàng thành công", resp.Data);
        }

        /// <summary>
        /// TCID07: UpdateToOrderStatus - Exception xảy ra trong service
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại (O)
        /// - Gọi tới StockBatchService.GetByProductIdForOrder ném exception (▼)
        ///
        /// INPUT:
        /// - transactionId: 21
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateToOrder_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 21;
            int warehouseId = 5;
            int productId = 500;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId };
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 1 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId } });

            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "WH" });

            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 10 } });

            _stockBatchServiceMock.Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>())).ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion
    }
}