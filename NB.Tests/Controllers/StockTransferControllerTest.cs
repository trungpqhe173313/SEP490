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
using NB.Service.Core;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Repository.Common;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using Xunit;
using NB.Test.Helpers;

namespace NB.Tests.Controllers
{
    public class StockTransferControllerTest
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
        private readonly Mock<ILogger<EmployeeController>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
        private readonly StockTransferController _controller;
        private readonly TransactionCodeGenerator _transactionCodeGenerator;

        public StockTransferControllerTest()
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
            _loggerMock = new Mock<ILogger<EmployeeController>>();
            _mapperMock = new Mock<IMapper>();
            _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
            _transactionRepositoryMock
                .Setup(r => r.GetQueryable())
                .Returns(new TestAsyncEnumerable<Transaction>(new List<Transaction>()));
            _transactionCodeGenerator = new TransactionCodeGenerator(_transactionRepositoryMock.Object);

            // Khởi tạo controller với các dependencies đã mock
            _controller = new StockTransferController(
                _transactionServiceMock.Object,
                _transactionDetailServiceMock.Object,
                _productServiceMock.Object,
                _stockBatchServiceMock.Object,
                _userServiceMock.Object,
                _warehouseServiceMock.Object,
                _inventoryServiceMock.Object,
                _returnTransactionServiceMock.Object,
                _returnTransactionDetailServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _transactionCodeGenerator
            );
        }

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
        /// - Warehouse tồn tại (cả kho nguồn và kho đích)
        /// - User tồn tại (cho ResponsibleId)
        /// - Transaction có Status
        /// 
        /// INPUT:
        /// - search: new TransactionSearch() (tối thiểu nhất, sẽ được set Type = "Transfer" trong code)
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 200 OK
        /// - Response: ApiResponse<PagedList<TransactionDto>>.Ok(result)
        /// - result.Items có data với WarehouseName, WarehouseInName, ResponsibleName, StatusName đầy đủ
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
                    WarehouseId = 1, // Kho nguồn
                    WarehouseInId = 2, // Kho đích
                    Status = 1,
                    TransactionDate = DateTime.Now,
                    TotalCost = 1000000,
                    ResponsibleId = 3 // Có người chịu trách nhiệm
                },
                new TransactionDto
                {
                    TransactionId = 2,
                    WarehouseId = 1, // Kho nguồn
                    WarehouseInId = null, // Không có kho đích
                    Status = 2,
                    TransactionDate = DateTime.Now,
                    TotalCost = 2000000,
                    ResponsibleId = null // Không có người chịu trách nhiệm
                }
            };

            var pagedResult = new PagedList<TransactionDto>(
                items: transactions,
                pageIndex: 1,
                pageSize: 10,
                totalCount: 2
            );

            // MOCK DATA: Warehouses tồn tại (cả kho nguồn và kho đích)
            var warehouses = new List<WarehouseDto?>
            {
                new WarehouseDto
                {
                    WarehouseId = 1,
                    WarehouseName = "Kho Hà Nội"
                },
                new WarehouseDto
                {
                    WarehouseId = 2,
                    WarehouseName = "Kho Hồ Chí Minh"
                }
            };

            // MOCK DATA: Users tồn tại (cho ResponsibleId)
            var users = new List<User>
            {
                new User
                {
                    UserId = 3,
                    FullName = "Lê Văn C",
                    Username = "levanc"
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
                .Setup(s => s.GetQueryable())
                .Returns(users.AsQueryable());

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: 200 OK với data đầy đủ
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<TransactionDto>>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Items);
            Assert.Equal(2, response.Data.Items.Count);
            
            // Verify data đầy đủ cho transaction đầu tiên
            Assert.Equal("Kho Hà Nội", response.Data.Items[0].WarehouseName);
            Assert.Equal("Kho Hồ Chí Minh", response.Data.Items[0].WarehouseInName);
            Assert.NotNull(response.Data.Items[0].StatusName);
            // Verify ResponsibleName được gắn đúng
            Assert.Equal(3, response.Data.Items[0].ResponsibleId);
            Assert.Equal("Lê Văn C", response.Data.Items[0].ResponsibleName);
            
            // Verify data cho transaction thứ hai
            Assert.Equal("Kho Hà Nội", response.Data.Items[1].WarehouseName);
            Assert.Null(response.Data.Items[1].WarehouseInId);
            Assert.NotNull(response.Data.Items[1].StatusName);
            // Verify transaction không có ResponsibleId thì ResponsibleName = null
            Assert.Null(response.Data.Items[1].ResponsibleId);
            Assert.Null(response.Data.Items[1].ResponsibleName);
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
        /// - Type: A (ABNormal)
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
                    WarehouseInId = 998,
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
                .ReturnsAsync((List<WarehouseDto?>?)null);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: 404 Not Found
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<WarehouseDto>>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho", response.Error.Message);
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

        #region GetDetail Tests

        /// <summary>
        /// TCID01: GetDetail với Id = 1 - Tất cả preconditions đều thỏa mãn
        /// 
        /// PRECONDITION:
        /// - Id > 0 (O)
        /// - Transaction với Id này tồn tại trong database (O)
        /// - Có ít nhất 1 product detail cho transaction này (O)
        /// - Products tồn tại cho các product details (O)
        /// - Warehouse nguồn và đích tồn tại (O)
        /// - ResponsibleId có giá trị và user tồn tại (O)
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
            int responsibleId = 1;

            // MOCK DATA: Transaction tồn tại
            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = 1,
                TransactionDate = DateTime.Now,
                WarehouseId = 1, // Kho nguồn
                WarehouseInId = 2, // Kho đích
                TotalWeight = 100,
                Note = "Ghi chú",
                ResponsibleId = 3 // Có người chịu trách nhiệm
            };

            // MOCK DATA: Warehouse nguồn tồn tại
            var sourceWarehouseDto = new WarehouseDto
            {
                WarehouseId = 1,
                WarehouseName = "Kho Hà Nội"
            };

            // MOCK DATA: Warehouse đích tồn tại
            var destWarehouseDto = new WarehouseDto
            {
                WarehouseId = 2,
                WarehouseName = "Kho Hồ Chí Minh"
            };

            // MOCK DATA: Responsible user tồn tại
            var responsibleUser = new UserDto
            {
                UserId = 3,
                FullName = "Lê Văn C",
                Username = "levanc"
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
                .ReturnsAsync(sourceWarehouseDto);

            _warehouseServiceMock
                .Setup(s => s.GetById(transactionDto.WarehouseInId.Value))
                .ReturnsAsync(destWarehouseDto);

            _userServiceMock
                .Setup(s => s.GetByUserId(transactionDto.ResponsibleId.Value))
                .ReturnsAsync(responsibleUser);

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
            var response = Assert.IsType<ApiResponse<FullTransactionTransferVM>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(transactionId, response.Data.TransactionId);
            
            // Verify có list transaction detail
            Assert.NotNull(response.Data.list);
            Assert.True(response.Data.list.Count >= 1); // Có ít nhất 1 product detail
            Assert.Equal(2, response.Data.list.Count);
            
            // Verify transaction information đầy đủ
            Assert.Equal(sourceWarehouseDto.WarehouseName, response.Data.SourceWarehouseName);
            Assert.Equal(destWarehouseDto.WarehouseName, response.Data.DestinationWarehouseName);
            // Verify ResponsibleName được gắn đúng
            Assert.Equal(3, response.Data.ResponsibleId);
            Assert.Equal("Lê Văn C", response.Data.ResponsibleName);
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
            var response = Assert.IsType<ApiResponse<FullTransactionTransferVM>>(badRequestResult.Value);
            
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
        /// - Return: Message = "Không tìm thấy đơn chuyển kho."
        /// - Type: A (Abnormal)
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
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert - EXPECTED OUTPUT: Message = "Không tìm thấy đơn chuyển kho."
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionTransferVM>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn chuyển kho.", response.Error.Message);
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
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found (code trả về 400 nhưng type là NotFoundObjectResult)
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
                WarehouseInId = 2,
                TotalWeight = 500
            };

            // MOCK DATA: Warehouse tồn tại
            var sourceWarehouseDto = new WarehouseDto
            {
                WarehouseId = 1,
                WarehouseName = "Kho Hà Nội"
            };

            var destWarehouseDto = new WarehouseDto
            {
                WarehouseId = 2,
                WarehouseName = "Kho Hồ Chí Minh"
            };

            // Setup mocks
            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            _warehouseServiceMock
                .Setup(s => s.GetById(transactionDto.WarehouseId))
                .ReturnsAsync(sourceWarehouseDto);

            _warehouseServiceMock
                .Setup(s => s.GetById(transactionDto.WarehouseInId.Value))
                .ReturnsAsync(destWarehouseDto);

            // KHÔNG có product details - trả về null hoặc empty
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((List<TransactionDetailDto>?)null);

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert - EXPECTED OUTPUT: Message = "Không có thông tin cho giao dịch này."
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionTransferVM>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không có thông tin cho giao dịch này.", response.Error.Message);
            Assert.Equal(400, response.StatusCode); // Code trả về 400 nhưng là NotFoundObjectResult
        }

        /// <summary>
        /// TCID05: GetDetail với input không hợp lệ (ModelState invalid)
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
        public async Task TCID06_GetDetail_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 1;
            int responsibleId = 1;

            // Setup mock để throw exception
            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetDetail(transactionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullTransactionTransferVM>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu", response.Error.Message);
        }

        #endregion

        #region CreateTransferOrder Tests

        /// <summary>
        /// TCID01: CreateTransferOrder với ListProductOrder null hoặc empty
        /// 
        /// PRECONDITION:
        /// - ListProductOrder == null hoặc empty
        /// 
        /// INPUT:
        /// - or: TransferRequest với ListProductOrder = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không có sản phẩm nào"
        /// - Type: A (Abnormal)
        /// - Status: 404 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_CreateTransferOrder_EmptyProductList_ReturnsBadRequest()
        {
            // Arrange - INPUT: ListProductOrder = null
            var transferRequest = new TransferRequest
            {
                ListProductOrder = null!,
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không có sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không có sản phẩm nào", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateTransferOrder với WarehouseId == WarehouseInId
        /// 
        /// PRECONDITION:
        /// - WarehouseId == WarehouseInId
        /// 
        /// INPUT:
        /// - or: TransferRequest với WarehouseId = WarehouseInId = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Kho nguồn và kho đích không thể giống nhau"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_CreateTransferOrder_SameWarehouse_ReturnsBadRequest()
        {
            // Arrange - INPUT: WarehouseId == WarehouseInId
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 1
            };

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Kho nguồn và kho đích không thể giống nhau"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Kho nguồn và kho đích không thể giống nhau", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreateTransferOrder với kho nguồn không tồn tại
        /// 
        /// PRECONDITION:
        /// - SourceWarehouse không tồn tại
        /// 
        /// INPUT:
        /// - or: TransferRequest với WarehouseId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho nguồn"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_CreateTransferOrder_SourceWarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: SourceWarehouse không tồn tại
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 999,
                WarehouseInId = 2
            };

            // Setup mock: SourceWarehouse không tồn tại
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy kho nguồn"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho nguồn", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreateTransferOrder với kho đích không tồn tại
        /// 
        /// PRECONDITION:
        /// - DestWarehouse không tồn tại
        /// 
        /// INPUT:
        /// - or: TransferRequest với WarehouseInId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho đích"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_CreateTransferOrder_DestWarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: DestWarehouse không tồn tại
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 999
            };

            // Setup mock: SourceWarehouse tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);

            // Setup mock: DestWarehouse không tồn tại
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy kho đích"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho đích", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreateTransferOrder với sản phẩm không tồn tại
        /// 
        /// PRECONDITION:
        /// - Products không tồn tại
        /// 
        /// INPUT:
        /// - or: TransferRequest với ProductId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm nào"
        /// - Type: N (Normal)
        /// - Status: 404 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_CreateTransferOrder_ProductsNotFound_ReturnsBadRequest()
        {
            // Arrange - INPUT: Products không tồn tại
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 999, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products không tồn tại
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto>()); // Empty list

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy sản phẩm nào", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CreateTransferOrder với ResponsibleId nhưng user không tồn tại
        /// 
        /// PRECONDITION:
        /// - ResponsibleId có giá trị nhưng user không tồn tại
        /// 
        /// INPUT:
        /// - or: TransferRequest với ResponsibleId = 999 (user không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy người chịu trách nhiệm với ID này"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID06_CreateTransferOrder_ResponsibleUserNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: ResponsibleId nhưng user không tồn tại
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2,
                ResponsibleId = 999
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: ResponsibleUser không tồn tại
            _userServiceMock
                .Setup(s => s.GetByUserId(999))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy người chịu trách nhiệm với ID này"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy người chịu trách nhiệm với ID này", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID07: CreateTransferOrder với sản phẩm không có trong kho nguồn
        /// 
        /// PRECONDITION:
        /// - Sản phẩm tồn tại
        /// - Kho nguồn tồn tại
        /// - Sản phẩm không có trong inventory của kho nguồn
        /// 
        /// INPUT:
        /// - or: TransferRequest với sản phẩm không có trong kho nguồn
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm '{productName}' trong kho nguồn '{warehouseName}'"
        /// - Type: N (Normal)
        /// - Status: 404 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_CreateTransferOrder_ProductNotInSourceWarehouse_ReturnsBadRequest()
        {
            // Arrange - INPUT: Sản phẩm không có trong kho nguồn
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Sản phẩm không có trong inventory của kho nguồn
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto>()); // Empty list

            // Setup mock: GetByIdAsync cho product name
            _productServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "Không tìm thấy sản phẩm"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<InventoryDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không tìm thấy sản phẩm", response.Error.Message);
            Assert.Contains("Kho Nguồn", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID08: CreateTransferOrder với số lượng không đủ trong kho nguồn
        /// 
        /// PRECONDITION:
        /// - Sản phẩm tồn tại
        /// - Kho nguồn tồn tại
        /// - Sản phẩm có trong kho nhưng số lượng không đủ
        /// 
        /// INPUT:
        /// - or: TransferRequest với Quantity > Quantity trong inventory
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Sản phẩm '{productName}' trong kho nguồn '{warehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu."
        /// - Type: N (Normal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_CreateTransferOrder_InsufficientQuantity_ReturnsBadRequest()
        {
            // Arrange - INPUT: Số lượng không đủ
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 100, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Inventory có số lượng ít hơn yêu cầu
            var inventoryDto = new InventoryDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 50 // Chỉ có 50, yêu cầu 100
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: GetByIdAsync cho product name
            _productServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

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
        /// TCID09: CreateTransferOrder với không tìm thấy lô hàng khả dụng
        /// 
        /// PRECONDITION:
        /// - Sản phẩm tồn tại và có trong kho
        /// - Số lượng đủ
        /// - Không có stock batch khả dụng (hết hạn hoặc đã hết hàng)
        /// 
        /// INPUT:
        /// - or: TransferRequest với sản phẩm không có stock batch khả dụng
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy lô hàng khả dụng cho sản phẩm '{productName}' trong kho nguồn"
        /// - Type: N (Normal)
        /// - Status: 404 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID09_CreateTransferOrder_NoAvailableStockBatch_ReturnsBadRequest()
        {
            // Arrange - INPUT: Không có stock batch khả dụng
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Inventory có đủ số lượng
            var inventoryDto = new InventoryDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 100
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Không có stock batch khả dụng (empty list hoặc không match điều kiện)
            // Code filter stock batch theo: WarehouseId == or.WarehouseId, QuantityIn > QuantityOut, ExpireDate > Today
            // Nếu empty list hoặc không match, stockBatchSourceByProduct sẽ không chứa productId
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto>()); // Empty list - không có stock batch

            // Setup mock: GetByIdAsync cho product name (được gọi khi trả về error message)
            _productServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(productDto);

            // Setup mock: Mapper để map TransactionCreateVM -> Transaction
            var transactionEntity = new Transaction
            {
                TransactionId = 1,
                WarehouseId = 1,
                WarehouseInId = 2,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit
            };
            _mapperMock
                .Setup(m => m.Map<TransactionCreateVM, Transaction>(It.IsAny<TransactionCreateVM>()))
                .Returns(transactionEntity);

            // Setup mock: Transaction service để tạo transaction (trong try block)
            _transactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy lô hàng khả dụng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không tìm thấy lô hàng khả dụng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID10: CreateTransferOrder với không đủ hàng trong các lô
        /// 
        /// PRECONDITION:
        /// - Sản phẩm tồn tại và có trong kho
        /// - Có stock batch nhưng tổng số lượng không đủ
        /// 
        /// INPUT:
        /// - or: TransferRequest với Quantity > tổng số lượng trong các stock batch
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không đủ hàng trong các lô cho sản phẩm '{productName}' trong kho nguồn"
        /// - Type: N (Normal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID10_CreateTransferOrder_InsufficientStockBatchQuantity_ReturnsBadRequest()
        {
            // Arrange - INPUT: Không đủ hàng trong các lô
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 100, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Inventory có đủ số lượng
            var inventoryDto = new InventoryDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 100
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Stock batch có số lượng ít hơn yêu cầu
            // Code filter stock batch theo: WarehouseId == or.WarehouseId, QuantityIn > QuantityOut, ExpireDate > Today
            // Sau đó lấy hàng từ các lô cũ nhất trước (FIFO), nếu remaining > 0 thì trả về BadRequest
            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                ProductId = 1,
                WarehouseId = 1,
                QuantityIn = 50, // Chỉ có 50, nhưng cần 100
                QuantityOut = 0,
                ImportDate = DateTime.Now.AddDays(-10),
                ExpireDate = DateTime.Now.AddDays(30) // Chưa hết hạn
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // Setup mock: GetByIdAsync cho product name (được gọi khi trả về error message)
            _productServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(productDto);

            // Setup mock: Mapper để map TransactionCreateVM -> Transaction
            var transactionEntity = new Transaction
            {
                TransactionId = 1,
                WarehouseId = 1,
                WarehouseInId = 2,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit
            };
            _mapperMock
                .Setup(m => m.Map<TransactionCreateVM, Transaction>(It.IsAny<TransactionCreateVM>()))
                .Returns(transactionEntity);

            // Setup mock: Transaction service để tạo transaction (trong try block)
            _transactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Setup mock: GetByName để tránh exception khi tạo batch code (sẽ không được gọi vì return sớm)
            _stockBatchServiceMock
                .Setup(s => s.GetByName(It.IsAny<string>()))
                .ReturnsAsync((StockBatchDto?)null);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không đủ hàng trong các lô"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không đủ hàng trong các lô", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID11: CreateTransferOrder thành công với ResponsibleId
        /// 
        /// PRECONDITION:
        /// - Tất cả điều kiện hợp lệ
        /// - ResponsibleId có giá trị và user tồn tại
        /// 
        /// INPUT:
        /// - or: TransferRequest với đầy đủ thông tin hợp lệ và ResponsibleId = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Chuyển kho thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID11_CreateTransferOrder_Success_WithResponsibleId_ReturnsOk()
        {
            // Arrange - INPUT: Đầy đủ thông tin hợp lệ với ResponsibleId
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2,
                ResponsibleId = 1,
                Note = "Test note"
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product", WeightPerUnit = 1.5m };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });
            _productServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(productDto);

            // Setup mock: ResponsibleUser tồn tại
            var userDto = new UserDto { UserId = 1, FullName = "Test User" };
            _userServiceMock
                .Setup(s => s.GetByUserId(1))
                .ReturnsAsync(userDto);

            // Setup mock: Inventory có đủ số lượng
            var inventoryDto = new InventoryDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 100
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Stock batch có đủ số lượng
            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                ProductId = 1,
                WarehouseId = 1,
                QuantityIn = 100,
                QuantityOut = 0,
                ImportDate = DateTime.Now.AddDays(-10),
                ExpireDate = DateTime.Now.AddDays(30)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // Setup mock: GetByIdAsync cho stock batch entity
            var stockBatchEntity = new StockBatch
            {
                BatchId = 1,
                ProductId = 1,
                WarehouseId = 1,
                QuantityIn = 100,
                QuantityOut = 0
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(stockBatchEntity);

            // Setup mock: GetByName cho batch code (không tồn tại)
            _stockBatchServiceMock
                .Setup(s => s.GetByName(It.IsAny<string>()))
                .ReturnsAsync((StockBatchDto?)null);

            // Setup mock: Inventory entities
            var sourceInventoryEntity = new Inventory
            {
                InventoryId = 1,
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 100
            };
            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(1, 1))
                .ReturnsAsync(sourceInventoryEntity);
            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(2, 1))
                .ReturnsAsync((Inventory?)null); // Chưa có inventory ở kho đích

            // Setup mock: Mapper
            var transactionEntity = new Transaction
            {
                TransactionId = 1,
                WarehouseId = 1,
                WarehouseInId = 2,
                Status = (int)TransactionStatus.inTransit
            };
            _mapperMock
                .Setup(m => m.Map<TransactionCreateVM, Transaction>(It.IsAny<TransactionCreateVM>()))
                .Returns(transactionEntity);
            _mapperMock
                .Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>()))
                .Returns(new TransactionDetail { Id = 1 });

            // Setup mock: Transaction service
            _transactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Stock batch service - CreateAsync
            _stockBatchServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<StockBatchDto>()))
                .Returns(Task.CompletedTask);
            _stockBatchServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Inventory service
            _inventoryServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);
            _inventoryServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<InventoryDto>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Transaction detail service
            _transactionDetailServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: Ok với message "Chuyển kho thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal("Chuyển kho thành công", response.Data);
        }

        /// <summary>
        /// TCID12: CreateTransferOrder khi có exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Exception được throw trong try block
        /// 
        /// INPUT:
        /// - or: TransferRequest hợp lệ nhưng service throw exception
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID12_CreateTransferOrder_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: TransferRequest hợp lệ nhưng service throw exception
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Products tồn tại
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { productDto });

            // Setup mock: Inventory có đủ số lượng
            var inventoryDto = new InventoryDto
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 100
            };
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { inventoryDto });

            // Setup mock: Stock batch có đủ số lượng
            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                ProductId = 1,
                WarehouseId = 1,
                QuantityIn = 100,
                QuantityOut = 0,
                ImportDate = DateTime.Now.AddDays(-10),
                ExpireDate = DateTime.Now.AddDays(30)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // Setup mock: Exception được throw khi tạo transaction
            _transactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateTransferOrder(transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi chuyển kho", response.Error.Message);
        }

        #endregion

        #region UpdateTransferOrder Tests

        /// <summary>
        /// TCID01: UpdateTransferOrder với ListProductOrder null hoặc empty
        /// 
        /// PRECONDITION:
        /// - ListProductOrder == null hoặc empty
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest với ListProductOrder = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn chuyển kho mới không có sản phẩm nào để cập nhật."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateTransferOrder_EmptyProductList_ReturnsBadRequest()
        {
            // Arrange - INPUT: ListProductOrder = null
            int transactionId = 1;
            int responsibleId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = null!,
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn chuyển kho mới không có sản phẩm nào để cập nhật."
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn chuyển kho mới không có sản phẩm nào để cập nhật.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateTransferOrder với WarehouseId == WarehouseInId
        /// 
        /// PRECONDITION:
        /// - WarehouseId == WarehouseInId
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest với WarehouseId = WarehouseInId = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Kho nguồn và kho đích không thể giống nhau"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateTransferOrder_SameWarehouse_ReturnsBadRequest()
        {
            // Arrange - INPUT: WarehouseId == WarehouseInId
            int transactionId = 1;
            int responsibleId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 1
            };

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Kho nguồn và kho đích không thể giống nhau"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Kho nguồn và kho đích không thể giống nhau", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateTransferOrder với transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999 (không tồn tại)
        /// - or: TransferRequest hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn chuyển kho"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateTransferOrder_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Transaction không tồn tại
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((Transaction?)null);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn chuyển kho"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn chuyển kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateTransferOrder với transaction không phải là Transfer type
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Type != "Transfer"
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn này không phải là đơn chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateTransferOrder_NotTransferType_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không phải là Transfer type
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Transaction tồn tại nhưng Type != "Transfer"
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Import", // Không phải "Transfer"
                Status = (int)TransactionStatus.inTransit
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn này không phải là đơn chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn này không phải là đơn chuyển kho", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateTransferOrder với transaction đã transferred
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = transferred
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không thể cập nhật đơn chuyển kho đã hoàn thành"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateTransferOrder_AlreadyTransferred_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã transferred
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Transaction đã transferred
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.transferred,
                ResponsibleId = 1
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không thể cập nhật đơn chuyển kho đã hoàn thành"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể cập nhật đơn chuyển kho đã hoàn thành", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateTransferOrder với transaction đã cancel
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = cancel
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không thể cập nhật đơn chuyển kho đã hủy"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateTransferOrder_AlreadyCanceled_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã cancel
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Transaction đã cancel
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.cancel,
                ResponsibleId = 1
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không thể cập nhật đơn chuyển kho đã hủy"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể cập nhật đơn chuyển kho đã hủy", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: UpdateTransferOrder với kho nguồn không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - SourceWarehouse không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest với WarehouseId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho nguồn"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateTransferOrder_SourceWarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: SourceWarehouse không tồn tại
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 999,
                WarehouseInId = 2
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: SourceWarehouse không tồn tại
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy kho nguồn"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho nguồn", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID08: UpdateTransferOrder với kho đích không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - DestWarehouse không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest với WarehouseInId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho đích"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID08_UpdateTransferOrder_DestWarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: DestWarehouse không tồn tại
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 999
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: SourceWarehouse tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);

            // Setup mock: DestWarehouse không tồn tại
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy kho đích"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho đích", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID09: UpdateTransferOrder với không tìm thấy chi tiết đơn chuyển kho
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - Không có transaction details
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy chi tiết đơn chuyển kho"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID09_UpdateTransferOrder_TransactionDetailsNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Không có transaction details
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Không có transaction details
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync((List<TransactionDetailDto>?)null);

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy chi tiết đơn chuyển kho"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy chi tiết đơn chuyển kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID10: UpdateTransferOrder thành công - cập nhật với sản phẩm giống nhau (số lượng không đổi)
        /// 
        /// PRECONDITION:
        /// - Tất cả điều kiện hợp lệ
        /// - Sản phẩm giống nhau và số lượng không đổi
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest với sản phẩm giống nhau và số lượng không đổi
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn chuyển kho thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID10_UpdateTransferOrder_Success_SameProductSameQuantity_ReturnsOk()
        {
            // Arrange - INPUT: Sản phẩm giống nhau và số lượng không đổi
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2,
                Note = "Updated note"
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Transaction details tồn tại
            var oldDetail = new TransactionDetailDto
            {
                Id = 1,
                TransactionId = 1,
                ProductId = 1,
                Quantity = 10
            };
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync(new List<TransactionDetailDto> { oldDetail });
            _transactionDetailServiceMock
                .Setup(s => s.DeleteRange(It.IsAny<List<TransactionDetailDto>>()))
                .Returns(Task.CompletedTask);
            _transactionDetailServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            // Setup mock: StockBatch ở kho đích
            _stockBatchServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync(new List<StockBatchDto>());

            // Setup mock: Product
            var productDto = new ProductDto { ProductId = 1, ProductName = "Test Product", WeightPerUnit = 1.5m };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(productDto);

            // Setup mock: Mapper
            _mapperMock
                .Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>()))
                .Returns(new TransactionDetail { Id = 1 });

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn chuyển kho thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal("Cập nhật đơn chuyển kho thành công", response.Data);
        }

        /// <summary>
        /// TCID11: UpdateTransferOrder khi có exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Exception được throw trong try block
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: TransferRequest hợp lệ nhưng service throw exception
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID11_UpdateTransferOrder_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: TransferRequest hợp lệ nhưng service throw exception
            int transactionId = 1;
            var transferRequest = new TransferRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                },
                WarehouseId = 1,
                WarehouseInId = 2
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: Warehouses tồn tại
            var sourceWarehouse = new WarehouseDto { WarehouseId = 1, WarehouseName = "Kho Nguồn" };
            var destWarehouse = new WarehouseDto { WarehouseId = 2, WarehouseName = "Kho Đích" };
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(sourceWarehouse);
            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(destWarehouse);

            // Setup mock: Transaction details tồn tại
            var oldDetail = new TransactionDetailDto
            {
                Id = 1,
                TransactionId = 1,
                ProductId = 1,
                Quantity = 10
            };
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync(new List<TransactionDetailDto> { oldDetail });

            // Setup mock: Exception được throw khi xóa transaction details
            _transactionDetailServiceMock
                .Setup(s => s.DeleteRange(It.IsAny<List<TransactionDetailDto>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateTransferOrder(transactionId, transferRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật đơn chuyển kho", response.Error.Message);
        }

        #endregion

        #region UpdateToTransferredStatus Tests

        /// <summary>
        /// TCID01: UpdateToTransferredStatus với transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn chuyển kho"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateToTransferredStatus_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction không tồn tại
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((Transaction?)null);

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn chuyển kho"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn chuyển kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateToTransferredStatus với transaction không phải là Transfer type
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Type != "Transfer"
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn này không phải là đơn chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateToTransferredStatus_NotTransferType_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không phải là Transfer type
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction tồn tại nhưng Type != "Transfer"
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Import", // Không phải "Transfer"
                Status = (int)TransactionStatus.inTransit
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn này không phải là đơn chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn này không phải là đơn chuyển kho", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateToTransferredStatus với transaction đã transferred
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = transferred
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn chuyển kho đã ở trạng thái hoàn thành"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateToTransferredStatus_AlreadyTransferred_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã transferred
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction đã transferred
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.transferred,
                ResponsibleId = responsibleId
            };
            _userServiceMock
                .Setup(s => s.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn chuyển kho đã ở trạng thái hoàn thành"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn chuyển kho đã ở trạng thái hoàn thành", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateToTransferredStatus với transaction đã cancel
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = cancel
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không thể cập nhật đơn chuyển kho đã hủy"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToTransferredStatus_AlreadyCanceled_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã cancel
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction đã cancel
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.cancel,
                ResponsibleId = responsibleId
            };
            _userServiceMock
                .Setup(s => s.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không thể cập nhật đơn chuyển kho đã hủy"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể cập nhật đơn chuyển kho đã hủy", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateToTransferredStatus với transaction không ở trạng thái inTransit
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Status != inTransit (ví dụ: draft)
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Chỉ có thể cập nhật trạng thái khi đơn đang ở trạng thái 'Đang Chuyển'"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateToTransferredStatus_NotInTransitStatus_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không ở trạng thái inTransit
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction ở trạng thái draft (không phải inTransit)
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.draft,
                ResponsibleId = responsibleId
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Chỉ có thể cập nhật trạng thái khi đơn đang ở trạng thái 'Đang Chuyển'"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Chỉ có thể cập nhật trạng thái khi đơn đang ở trạng thái 'Đang Chuyển'", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateToTransferredStatus thành công
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại, Type = "Transfer", Status = inTransit
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn chuyển kho thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateToTransferredStatus_Success_ReturnsOk()
        {
            // Arrange - INPUT: Transaction hợp lệ
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction tồn tại, Type = "Transfer", Status = inTransit
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                ResponsibleId = responsibleId
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn chuyển kho thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal("Cập nhật đơn chuyển kho thành công", response.Data);

            // Verify: Status đã được cập nhật thành transferred
            _transactionServiceMock.Verify(s => s.UpdateAsync(It.Is<Transaction>(t => 
                t.TransactionId == 1 && 
                t.Status == (int)TransactionStatus.transferred)), Times.Once);
        }

        /// <summary>
        /// TCID07: UpdateToTransferredStatus khi có exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Exception được throw trong try block
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateToTransferredStatus_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction hợp lệ nhưng service throw exception
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToTransferredStatusRequest { ResponsibleId = responsibleId };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                ResponsibleId = responsibleId
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: Exception được throw khi update
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateToTransferredStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn chuyển kho", response.Error.Message);
        }

        #endregion

        #region CancelTransferOrder Tests

        /// <summary>
        /// TCID01: CancelTransferOrder với transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn chuyển kho"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_CancelTransferOrder_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;

            // Setup mock: Transaction không tồn tại
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((Transaction?)null);

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn chuyển kho"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn chuyển kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CancelTransferOrder với transaction không phải là Transfer type
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Type != "Transfer"
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn này không phải là đơn chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_CancelTransferOrder_NotTransferType_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không phải là Transfer type
            int transactionId = 1;

            // Setup mock: Transaction tồn tại nhưng Type != "Transfer"
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Import", // Không phải "Transfer"
                Status = (int)TransactionStatus.inTransit
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn này không phải là đơn chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn này không phải là đơn chuyển kho", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CancelTransferOrder với transaction đã transferred
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = transferred
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không thể hủy đơn chuyển kho đã hoàn thành"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_CancelTransferOrder_AlreadyTransferred_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã transferred
            int transactionId = 1;

            // Setup mock: Transaction đã transferred
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.transferred
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không thể hủy đơn chuyển kho đã hoàn thành"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể hủy đơn chuyển kho đã hoàn thành", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CancelTransferOrder với transaction đã cancel
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = cancel
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn chuyển kho đã được hủy trước đó"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_CancelTransferOrder_AlreadyCanceled_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã cancel
            int transactionId = 1;

            // Setup mock: Transaction đã cancel
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.cancel
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn chuyển kho đã được hủy trước đó"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn chuyển kho đã được hủy trước đó", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CancelTransferOrder với transaction details không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - Transaction details không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy chi tiết đơn chuyển kho"
        /// - Type: N (Normal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID05_CancelTransferOrder_TransactionDetailsNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction details không tồn tại
            int transactionId = 1;

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: Transaction details không tồn tại
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync((List<TransactionDetailDto>?)null);

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy chi tiết đơn chuyển kho"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy chi tiết đơn chuyển kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CancelTransferOrder thành công
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại, Type = "Transfer", Status = inTransit
        /// - Transaction details tồn tại
        /// - Stock batches tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Hủy đơn chuyển kho thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_CancelTransferOrder_Success_ReturnsOk()
        {
            // Arrange - INPUT: Transaction hợp lệ
            int transactionId = 1;

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Transaction details tồn tại
            var transactionDetail = new TransactionDetailDto
            {
                Id = 1,
                TransactionId = 1,
                ProductId = 1,
                Quantity = 10
            };
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync(new List<TransactionDetailDto> { transactionDetail });

            // Setup mock: Stock batches ở kho đích
            var destStockBatch = new StockBatchDto
            {
                BatchId = 1,
                ProductId = 1,
                WarehouseId = 2,
                QuantityIn = 10,
                QuantityOut = 0
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync(new List<StockBatchDto> { destStockBatch });

            // Setup mock: Inventory ở kho nguồn
            var sourceInventory = new Inventory
            {
                InventoryId = 1,
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 50
            };
            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(1, 1))
                .ReturnsAsync(sourceInventory);
            _inventoryServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Inventory ở kho đích
            var destInventory = new Inventory
            {
                InventoryId = 2,
                ProductId = 1,
                WarehouseId = 2,
                Quantity = 10
            };
            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(2, 1))
                .ReturnsAsync(destInventory);

            // Setup mock: Stock batch ở kho nguồn để revert
            var sourceStockBatchDto = new StockBatchDto
            {
                BatchId = 2,
                ProductId = 1,
                WarehouseId = 1,
                QuantityIn = 100,
                QuantityOut = 10,
                ImportDate = DateTime.Now.AddDays(-5)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { sourceStockBatchDto });

            var sourceStockBatchEntity = new StockBatch
            {
                BatchId = 2,
                ProductId = 1,
                WarehouseId = 1,
                QuantityIn = 100,
                QuantityOut = 10
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(sourceStockBatchEntity);
            _stockBatchServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Xóa stock batch ở kho đích
            var destStockBatchEntity = new StockBatch
            {
                BatchId = 1,
                ProductId = 1,
                WarehouseId = 2,
                QuantityIn = 10,
                QuantityOut = 0
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(destStockBatchEntity);
            _stockBatchServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: Ok với message "Hủy đơn chuyển kho thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal("Hủy đơn chuyển kho thành công", response.Data);

            // Verify: Status đã được cập nhật thành cancel
            _transactionServiceMock.Verify(s => s.UpdateAsync(It.Is<Transaction>(t => 
                t.TransactionId == 1 && 
                t.Status == (int)TransactionStatus.cancel)), Times.Once);
        }

        /// <summary>
        /// TCID07: CancelTransferOrder khi có exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Exception được throw trong try block
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi hủy đơn chuyển kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_CancelTransferOrder_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction hợp lệ nhưng service throw exception
            int transactionId = 1;

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new Transaction
            {
                TransactionId = 1,
                Type = "Transfer",
                Status = (int)TransactionStatus.inTransit,
                WarehouseId = 1,
                WarehouseInId = 2
            };
            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(transaction);

            // Setup mock: Transaction details tồn tại
            var transactionDetail = new TransactionDetailDto
            {
                Id = 1,
                TransactionId = 1,
                ProductId = 1,
                Quantity = 10
            };
            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ReturnsAsync(new List<TransactionDetailDto> { transactionDetail });

            // Setup mock: Exception được throw khi lấy stock batches
            _stockBatchServiceMock
                .Setup(s => s.GetByTransactionId(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CancelTransferOrder(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi hủy đơn chuyển kho"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi hủy đơn chuyển kho", response.Error.Message);
        }

        #endregion
    }
}

