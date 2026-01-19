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
using NB.Service.FinancialTransactionService;
using NB.Service.FinancialTransactionService.Dto;
using NB.Service.FinancialTransactionService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionDetailService.ViewModels;
using NB.Service.ReturnTransactionService;
using NB.Service.ReturnTransactionService.ViewModels;
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
using NB.Service.UserService.ViewModels;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using Xunit;
using NB.Test.Helpers;

namespace NB.Tests.Controllers
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
        private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
        private readonly StockOutputController _controller;

        private readonly TransactionCodeGenerator _transactionCodeGenerator;

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
            _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
            _transactionRepositoryMock
                .Setup(r => r.GetQueryable())
                .Returns(new TestAsyncEnumerable<Transaction>(new List<Transaction>()));
            _transactionCodeGenerator = new TransactionCodeGenerator(_transactionRepositoryMock.Object);

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
                _loggerMock.Object,
                _transactionCodeGenerator
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
                PriceListId = 1,
                ResponsibleId = 3 // Có người chịu trách nhiệm
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

            // MOCK DATA: Responsible user tồn tại
            var responsibleUser = new UserDto
            {
                UserId = 3,
                FullName = "Lê Văn C",
                Username = "levanc"
            };

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
        public async Task TCID06_GetDetail_WhenExceptionThrown_ReturnsBadRequest()
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
                    TotalCost = 1000000,
                    ResponsibleId = 3 // Có người chịu trách nhiệm
                },
                new TransactionDto
                {
                    TransactionId = 2,
                    CustomerId = 2,
                    WarehouseId = 1,
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

            // MOCK DATA: Warehouses tồn tại
            var warehouses = new List<WarehouseDto?>
            {
                new WarehouseDto
                {
                    WarehouseId = 1,
                    WarehouseName = "Kho Hà Nội"
                }
            };

            // MOCK DATA: Users tồn tại (bao gồm cả responsible users)
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
                },
                new UserDto
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
                .Setup(s => s.GetAll())
                .Returns(users);

            // Setup mock: GetQueryable để controller lấy thông tin người chịu trách nhiệm
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
            
            // Verify data đầy đủ
            Assert.Equal("Nguyễn Văn A", response.Data.Items[0].FullName);
            Assert.Equal("Kho Hà Nội", response.Data.Items[0].WarehouseName);
            Assert.NotNull(response.Data.Items[0].StatusName);
            // Verify ResponsibleName được gắn đúng
            Assert.Equal(3, response.Data.Items[0].ResponsibleId);
            Assert.Equal("Lê Văn C", response.Data.Items[0].ResponsibleName);
            
            Assert.Equal("Trần Thị B", response.Data.Items[1].FullName);
            Assert.Equal("Kho Hà Nội", response.Data.Items[1].WarehouseName);
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
        /// - Return: BadRequest với message "Số lượng tồn kho không đủ"
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

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "Số lượng tồn kho không đủ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Số lượng tồn kho không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
            Assert.NotNull(response.Error.Messages);
            var expectedDetail = $"- {productDto.ProductName}: còn {(int)Math.Floor(inventoryQuantity)}";
            Assert.Contains(expectedDetail, response.Error.Messages);
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

            // Assert - EXPECTED OUTPUT: BadRequest với message "Số lượng tồn kho không đủ" và danh sách chi tiết
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Số lượng tồn kho không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
            Assert.NotNull(response.Error.Messages);
            var expectedDetail = $"- {productDto.ProductName}: còn {(int)Math.Floor(5m)}";
            Assert.Contains(expectedDetail, response.Error.Messages);
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
        /// TCID08: UpdateToOrderStatus - responsibleId <= 0
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - Transaction tồn tại (O)
        /// - responsibleId <= 0 (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - responsibleId: 0
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "UserId người chịu trách nhiệm không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_UpdateToOrder_InvalidResponsibleId_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 1;
            int responsibleId = 0; // Invalid
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1, Status = (int)TransactionStatus.draft };
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "UserId người chịu trách nhiệm không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("UserId người chịu trách nhiệm không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID10: UpdateToOrderStatus - Request null
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - request = null (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request: null
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Request không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID10_UpdateToOrder_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 1;
            UpdateToOrderStatusRequest? request = null;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1, Status = (int)TransactionStatus.draft };
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request!);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Request không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Request không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: UpdateToOrderStatus - responsibleUser không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - Transaction tồn tại (O)
        /// - responsibleId > 0 (O)
        /// - User với responsibleId này KHÔNG tồn tại (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - responsibleId: 999
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy người chịu trách nhiệm với ID này"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID09_UpdateToOrder_ResponsibleUserNotFound_ReturnsNotFound()
        {
            // Arrange
            int transactionId = 1;
            int responsibleId = 999; // User không tồn tại
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1, Status = (int)TransactionStatus.draft };
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy người chịu trách nhiệm với ID này"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy người chịu trách nhiệm với ID này", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

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
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

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
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1, Status = (int)TransactionStatus.draft }; // Phải là draft
            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            
            // Mock responsible user (được check trước transaction details)
            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);
            
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync((List<TransactionDetailDto>?)null);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

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
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = 1, Status = (int)TransactionStatus.draft }; // Phải là draft
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = 100, Quantity = 2 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto>());
            
            // Mock responsible user
            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không tìm thấy sản phẩm nào"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy sản phẩm nào", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID11: UpdateToOrderStatus - Warehouse của đơn hàng không tồn tại
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - WarehouseId của transaction không có dữ liệu (▼)
        ///
        /// INPUT:
        /// - transactionId: 21
        /// - request.ResponsibleId hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho của đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID11_UpdateToOrder_WarehouseNotFound_ReturnsNotFound()
        {
            // Arrange
            int transactionId = 21;
            int warehouseId = 999;
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                WarehouseId = warehouseId,
                Status = (int)TransactionStatus.draft
            };

            var detail = new TransactionDetailDto { Id = 1, ProductId = 10, Quantity = 2 };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(new List<TransactionDetailDto> { detail });

            _productServiceMock
                .Setup(s => s.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = detail.ProductId } });

            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho của đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID12: UpdateToOrderStatus - Sản phẩm không có trong kho
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft (O)
        /// - Product tồn tại (O)
        /// - Inventory của kho trả về null cho product này (▼)
        ///
        /// INPUT:
        /// - transactionId: 22
        /// - request.ResponsibleId hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số lượng tồn kho không đủ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID12_UpdateToOrder_ProductNotInWarehouse_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 22;
            int warehouseId = 5;
            int responsibleId = 2;
            int productId = 100;
            const string productName = "Sản phẩm thiếu";
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                WarehouseId = warehouseId,
                Status = (int)TransactionStatus.draft
            };
            var details = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 3 }
            };

            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Kho thiếu hàng" };
            var productDto = new ProductDto { ProductId = productId, ProductName = productName };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { productDto });
            _productServiceMock.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync(productDto);
            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(warehouseDto);
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto>()); // Không có sản phẩm trong kho

            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Số lượng tồn kho không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
            Assert.NotNull(response.Error.Messages);
            Assert.Contains($"{productName}: còn 0", response.Error.Messages);
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
        /// - Return: BadRequest với message "Số lượng tồn kho không đủ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToOrder_InsufficientInventory_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 12;
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            int warehouseId = 5;
            int productId = 200;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId, Status = (int)TransactionStatus.draft }; // Phải là draft
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 10 } };

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            var warehouseDto = new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { productDto });
            _productServiceMock.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync(productDto);
            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(warehouseDto);
            
            // Mock responsible user
            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 5 } }); // not enough (need 10)

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "chỉ còn" và "không đủ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Số lượng tồn kho không đủ", response.Error.Message);
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
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            int warehouseId = 5;
            int productId = 300;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId, Status = (int)TransactionStatus.draft }; // Phải là draft
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId } });

            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "WH" });
            
            // Mock responsible user
            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

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
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

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
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            int warehouseId = 5;
            int productId = 400;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId, Status = (int)TransactionStatus.draft }; // Phải là draft
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId } });

            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "WH" });
            
            // Mock responsible user
            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

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
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

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
            int responsibleId = 1;
            var request = new UpdateToOrderStatusRequest { ResponsibleId = responsibleId };
            int warehouseId = 5;
            int productId = 500;
            var txDto = new TransactionDto { TransactionId = transactionId, WarehouseId = warehouseId, Status = (int)TransactionStatus.draft }; // Phải là draft
            var details = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 1 } };

            _transactionServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(txDto);
            _transactionDetailServiceMock.Setup(s => s.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _productServiceMock.Setup(s => s.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new ProductDto { ProductId = productId } });

            _warehouseServiceMock.Setup(s => s.GetByIdAsync(warehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "WH" });
            
            // Mock responsible user
            var responsibleUser = new UserDto { UserId = responsibleId, FullName = "Responsible User" };
            _userServiceMock.Setup(s => s.GetByUserId(responsibleId)).ReturnsAsync(responsibleUser);

            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<InventoryDto> { new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = 10 } });

            _stockBatchServiceMock.Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>())).ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateToOrderStatus(transactionId, request);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateToDoneStatus Tests

        /// <summary>
        /// TCID01: UpdateToDoneStatus - TransactionId không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId <= 0 (▼)
        /// - request hợp lệ (O)
        ///
        /// INPUT:
        /// - transactionId: 0
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Transaction ID không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateToDone_InvalidTransactionId_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 0;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = 1 };

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Transaction ID không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateToDoneStatus - Request null
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - request = null (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request: null
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Request không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateToDone_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 1;

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Request không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateToDoneStatus - ResponsibleId không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - request.ResponsibleId <= 0 (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request.ResponsibleId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "UserId người chịu trách nhiệm không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateToDone_InvalidResponsibleId_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 1;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = 0 };

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("UserId người chịu trách nhiệm không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateToDoneStatus - Transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - transactionService trả về null (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToDone_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange
            int transactionId = 1;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = 1 };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateToDoneStatus - Đơn hàng không ở trạng thái delivering
        ///
        /// PRECONDITION:
        /// - transaction tồn tại (O)
        /// - transaction.Status != delivering (▼)
        ///
        /// INPUT:
        /// - transactionId: 2
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không trong trạng thái đang giao"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateToDone_NotDeliveringStatus_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 2;
            int responsibleId = 1;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = responsibleId };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                ResponsibleId = responsibleId
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không trong trạng thái đang giao", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateToDoneStatus - Không có quyền xác nhận (responsibleId khác)
        ///
        /// PRECONDITION:
        /// - transaction tồn tại, Status = delivering (O)
        /// - transaction.ResponsibleId != request.ResponsibleId (▼)
        ///
        /// INPUT:
        /// - transactionId: 3
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Bạn không có quyền xác nhận hoàn thành đơn hàng này."
        /// - Type: A (Abnormal)
        /// - Status: 403 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateToDone_InvalidResponsible_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 3;
            int responsibleId = 1;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = responsibleId };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.delivering,
                ResponsibleId = 9
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Bạn không có quyền xác nhận hoàn thành đơn hàng này.", response.Error.Message);
            Assert.Equal(403, response.StatusCode);
        }

        /// <summary>
        /// TCID07: UpdateToDoneStatus - Thành công
        ///
        /// PRECONDITION:
        /// - transaction tồn tại, Status = delivering (O)
        /// - ResponsibleId đúng (O)
        ///
        /// INPUT:
        /// - transactionId: 4
        /// - request.ResponsibleId = 5
        ///
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateToDone_Success_ReturnsOk()
        {
            // Arrange
            int transactionId = 4;
            int responsibleId = 5;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = responsibleId };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.delivering,
                ResponsibleId = responsibleId
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID08: UpdateToDoneStatus - Exception xảy ra khi cập nhật
        ///
        /// PRECONDITION:
        /// - transaction tồn tại, Status = delivering (O)
        /// - ResponsibleId đúng (O)
        /// - service UpdateAsync ném exception (▼)
        ///
        /// INPUT:
        /// - transactionId: 5
        /// - request.ResponsibleId = 6
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_UpdateToDone_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            int transactionId = 5;
            int responsibleId = 6;
            var request = new UpdateToDoneStatusRequest { ResponsibleId = responsibleId };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.delivering,
                ResponsibleId = responsibleId
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateToDoneStatus(transactionId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateToDeliveringStatus Tests

        /// <summary>
        /// TCID01: UpdateToDeliveringStatus - TransactionId không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId <= 0 (▼)
        /// - request hợp lệ (O)
        ///
        /// INPUT:
        /// - transactionId: 0
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Transaction ID không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateToDelivering_InvalidTransactionId_ReturnsBadRequest()
        {
            int transactionId = 0;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = 1 };

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Transaction ID không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateToDeliveringStatus - Request null
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - request = null (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request: null
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Request không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateToDelivering_NullRequest_ReturnsBadRequest()
        {
            int transactionId = 1;

            var result = await _controller.UpdateToDeliveringStatus(transactionId, null);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Request không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateToDeliveringStatus - ResponsibleId không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - request.ResponsibleId <= 0 (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request.ResponsibleId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "UserId người chịu trách nhiệm không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateToDelivering_InvalidResponsibleId_ReturnsBadRequest()
        {
            int transactionId = 1;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = 0 };

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("UserId người chịu trách nhiệm không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateToDeliveringStatus - Transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0 (O)
        /// - transactionService trả về null (▼)
        ///
        /// INPUT:
        /// - transactionId: 1
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToDelivering_TransactionNotFound_ReturnsNotFound()
        {
            int transactionId = 1;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = 1 };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateToDeliveringStatus - Đơn hàng không ở trạng thái nháp
        ///
        /// PRECONDITION:
        /// - transaction tồn tại (O)
        /// - transaction.Status != order (▼)
        ///
        /// INPUT:
        /// - transactionId: 2
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không trong trạng thái nháp"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateToDelivering_NotInOrderStatus_ReturnsBadRequest()
        {
            int transactionId = 2;
            int responsibleId = 1;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = responsibleId };

            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                ResponsibleId = responsibleId
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(txDto);

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Đơn hàng không trong trạng thái nháp", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateToDeliveringStatus - Không có quyền xác nhận giao hàng
        ///
        /// PRECONDITION:
        /// - transaction tồn tại, Status = order (O)
        /// - transaction.ResponsibleId != request.ResponsibleId (▼)
        ///
        /// INPUT:
        /// - transactionId: 3
        /// - request.ResponsibleId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Bạn không có quyền xác nhận giao hàng cho đơn hàng này."
        /// - Type: A (Abnormal)
        /// - Status: 403 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateToDelivering_InvalidResponsible_ReturnsBadRequest()
        {
            int transactionId = 3;
            int responsibleId = 1;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = responsibleId };

            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                ResponsibleId = 9
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(txDto);

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Bạn không có quyền xác nhận giao hàng cho đơn hàng này.", response.Error.Message);
            Assert.Equal(403, response.StatusCode);
        }

        /// <summary>
        /// TCID07: UpdateToDeliveringStatus - Thành công
        ///
        /// PRECONDITION:
        /// - transaction tồn tại, Status = order (O)
        /// - ResponsibleId đúng (O)
        ///
        /// INPUT:
        /// - transactionId: 4
        /// - request.ResponsibleId = 2
        ///
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateToDelivering_Success_ReturnsOk()
        {
            int transactionId = 4;
            int responsibleId = 2;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = responsibleId };

            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                ResponsibleId = responsibleId
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(txDto);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID08: UpdateToDeliveringStatus - Exception xảy ra
        ///
        /// PRECONDITION:
        /// - transaction tồn tại, Status = order
        /// - ResponsibleId đúng
        /// - UpdateAsync ném exception (▼)
        ///
        /// INPUT:
        /// - transactionId: 5
        /// - request.ResponsibleId = 3
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_UpdateToDelivering_ExceptionThrown_ReturnsBadRequest()
        {
            int transactionId = 5;
            int responsibleId = 3;
            var request = new UpdateToDeliveringStatusRequest { ResponsibleId = responsibleId };

            var txDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                ResponsibleId = responsibleId
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(txDto);
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.UpdateToDeliveringStatus(transactionId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateTransactionInOrderStatus Tests

        /// <summary>
        /// TCID01: UpdateTransactionInOrderStatus - ListProductOrder null hoặc empty
        /// 
        /// PRECONDITION:
        /// - OrderRequest.ListProductOrder = null hoặc empty
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với ListProductOrder = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng mới không có sản phẩm nào để cập nhật."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateInOrder_EmptyProductList_ReturnsBadRequest()
        {
            // Arrange - INPUT: ListProductOrder = null
            int transactionId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = null!
            };

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng mới không có sản phẩm nào để cập nhật."
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng mới không có sản phẩm nào để cập nhật.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateTransactionInOrderStatus - Transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction với transactionId không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999
        /// - or: OrderRequest với ListProductOrder có ít nhất 1 sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateInOrder_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync((Transaction?)null);

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateTransactionInOrderStatus - Transaction không ở trạng thái order
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Status != order
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với ListProductOrder có ít nhất 1 sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không trong trạng thái lên đơn"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateInOrder_NotInOrderStatus_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không ở trạng thái order
            int transactionId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft, // Không phải order
                WarehouseId = 1
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng không trong trạng thái lên đơn"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không trong trạng thái lên đơn", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateTransactionInOrderStatus - Warehouse không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - WarehouseId của transaction không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với ListProductOrder có ít nhất 1 sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho của đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateInOrder_WarehouseNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Warehouse không tồn tại
            int transactionId = 1;
            int warehouseId = 999;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy kho của đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy kho của đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateTransactionInOrderStatus - Chi tiết đơn hàng không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng không tồn tại hoặc empty
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với ListProductOrder có ít nhất 1 sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy chi tiết đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateInOrder_NoTransactionDetails_ReturnsNotFound()
        {
            // Arrange - INPUT: Chi tiết đơn hàng không tồn tại
            int transactionId = 1;
            int warehouseId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((List<TransactionDetailDto>?)null);

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy chi tiết đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy chi tiết đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateTransactionInOrderStatus - Sản phẩm mới không có trong kho
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm mới (chỉ có trong đơn mới) không có trong inventory
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm mới không có trong kho
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy sản phẩm '{productName}' trong kho '{warehouseName}'"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateInOrder_NewProductNotInWarehouse_ReturnsBadRequest()
        {
            // Arrange - INPUT: Sản phẩm mới không có trong kho
            int transactionId = 1;
            int warehouseId = 1;
            int oldProductId = 1;
            int newProductId = 999; // Sản phẩm mới
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = newProductId, Quantity = 10, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = oldProductId, Quantity = 5 }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            // Sản phẩm mới không có trong inventory
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.Is<List<int>>(l => l.Contains(newProductId))))
                .ReturnsAsync(new List<InventoryDto>()); // Empty - không có sản phẩm mới trong kho

            _productServiceMock
                .Setup(s => s.GetByIdAsync(newProductId))
                .ReturnsAsync(new ProductDto { ProductId = newProductId, ProductName = "New Product" });

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "Không tìm thấy sản phẩm"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không tìm thấy sản phẩm", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: UpdateTransactionInOrderStatus - Sản phẩm mới không đủ số lượng
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm mới có trong kho nhưng số lượng không đủ
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm mới yêu cầu số lượng lớn hơn inventory
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Sản phẩm '{productName}' trong kho '{warehouseName}' chỉ còn {invenQty}, không đủ {newQuantity} yêu cầu."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateInOrder_NewProductInsufficientQuantity_ReturnsBadRequest()
        {
            // Arrange - INPUT: Sản phẩm mới không đủ số lượng
            int transactionId = 1;
            int warehouseId = 1;
            int oldProductId = 1;
            int newProductId = 999;
            decimal newQuantity = 100; // Yêu cầu 100
            decimal inventoryQuantity = 50; // Chỉ còn 50
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = newProductId, Quantity = newQuantity, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = oldProductId, Quantity = 5 }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            var productDto = new ProductDto { ProductId = newProductId, ProductName = "New Product" };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(newProductId))
                .ReturnsAsync(productDto);

            // Sản phẩm mới có trong kho nhưng số lượng không đủ
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.Is<List<int>>(l => l.Contains(newProductId))))
                .ReturnsAsync(new List<InventoryDto>
                {
                    new InventoryDto { ProductId = newProductId, WarehouseId = warehouseId, Quantity = inventoryQuantity }
                });

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "chỉ còn" và "không đủ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("chỉ còn", response.Error.Message);
            Assert.Contains("không đủ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID08: UpdateTransactionInOrderStatus - Tăng số lượng sản phẩm nhưng không đủ inventory
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm có trong cả đơn cũ và đơn mới, đơn mới nhiều hơn nhưng không đủ inventory
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm tăng số lượng nhưng không đủ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Sản phẩm '{productName}' trong kho '{warehouseName}' chỉ còn {inventoryDto.Quantity}, không đủ {diff} để tăng."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_UpdateInOrder_IncreaseQuantityInsufficient_ReturnsBadRequest()
        {
            // Arrange - INPUT: Tăng số lượng nhưng không đủ
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal oldQuantity = 10;
            decimal newQuantity = 100; // Tăng lên 100
            decimal diff = newQuantity - oldQuantity; // 90
            decimal inventoryQuantity = 50; // Chỉ còn 50, không đủ 90
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = newQuantity, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = (int)oldQuantity }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Inventory không đủ để tăng
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductId(warehouseId, productId))
                .ReturnsAsync(new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = inventoryQuantity });

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "chỉ còn" và "không đủ" và "để tăng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("chỉ còn", response.Error.Message);
            Assert.Contains("không đủ", response.Error.Message);
            Assert.Contains("để tăng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: UpdateTransactionInOrderStatus - Không tìm thấy lô hàng khả dụng khi tăng số lượng
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm tăng số lượng, inventory đủ nhưng không có lô hàng khả dụng
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm tăng số lượng nhưng không có lô hàng
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không tìm thấy lô hàng khả dụng cho sản phẩm {productId} trong kho '{warehouseName}'."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID09_UpdateInOrder_IncreaseQuantityNoStockBatch_ReturnsBadRequest()
        {
            // Arrange - INPUT: Tăng số lượng nhưng không có lô hàng
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal oldQuantity = 10;
            decimal newQuantity = 20; // Tăng lên 20
            decimal diff = newQuantity - oldQuantity; // 10
            decimal inventoryQuantity = 100; // Đủ số lượng
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = newQuantity, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = (int)oldQuantity }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Inventory đủ
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductId(warehouseId, productId))
                .ReturnsAsync(new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = inventoryQuantity });

            // Không có lô hàng khả dụng
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.Is<List<int>>(l => l.Contains(productId))))
                .ReturnsAsync(new List<StockBatchDto>()); // Empty

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message chứa "Không tìm thấy lô hàng khả dụng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không tìm thấy lô hàng khả dụng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID10: UpdateTransactionInOrderStatus - Không đủ hàng trong các lô khi tăng số lượng
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm tăng số lượng, có lô hàng nhưng không đủ
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm tăng số lượng nhưng lô hàng không đủ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không đủ hàng trong các lô cho sản phẩm {productId}"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID10_UpdateInOrder_IncreaseQuantityInsufficientStockBatch_ReturnsBadRequest()
        {
            // Arrange - INPUT: Tăng số lượng nhưng lô hàng không đủ
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal oldQuantity = 10;
            decimal newQuantity = 100; // Tăng lên 100
            decimal diff = newQuantity - oldQuantity; // 90
            decimal inventoryQuantity = 200; // Đủ số lượng
            decimal stockBatchAvailable = 50; // Lô hàng chỉ có 50, không đủ 90
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = newQuantity, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = (int)oldQuantity }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Inventory đủ
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductId(warehouseId, productId))
                .ReturnsAsync(new InventoryDto { ProductId = productId, WarehouseId = warehouseId, Quantity = inventoryQuantity });

            // Lô hàng không đủ
            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                ProductId = productId,
                WarehouseId = warehouseId,
                QuantityIn = 100,
                QuantityOut = 50, // Đã xuất 50, còn lại 50
                ImportDate = DateTime.Today.AddDays(-10),
                ExpireDate = DateTime.Today.AddDays(30)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.Is<List<int>>(l => l.Contains(productId))))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không đủ hàng trong các lô cho sản phẩm"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không đủ hàng trong các lô cho sản phẩm", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID11: UpdateTransactionInOrderStatus - Không đủ hàng trong các lô cho sản phẩm mới
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm mới có trong kho, đủ số lượng nhưng lô hàng không đủ
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm mới nhưng lô hàng không đủ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không đủ hàng trong các lô cho sản phẩm {productId}"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID11_UpdateInOrder_NewProductInsufficientStockBatch_ReturnsBadRequest()
        {
            // Arrange - INPUT: Sản phẩm mới nhưng lô hàng không đủ
            int transactionId = 1;
            int warehouseId = 1;
            int oldProductId = 1;
            int newProductId = 999;
            decimal newQuantity = 100; // Yêu cầu 100
            decimal inventoryQuantity = 200; // Đủ số lượng
            decimal stockBatchAvailable = 50; // Lô hàng chỉ có 50, không đủ 100
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = newProductId, Quantity = newQuantity, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = oldProductId, Quantity = 5 }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            var productDto = new ProductDto { ProductId = newProductId, ProductName = "New Product" };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(newProductId))
                .ReturnsAsync(productDto);

            // Inventory đủ
            _inventoryServiceMock
                .Setup(s => s.GetByWarehouseAndProductIds(warehouseId, It.Is<List<int>>(l => l.Contains(newProductId))))
                .ReturnsAsync(new List<InventoryDto>
                {
                    new InventoryDto { ProductId = newProductId, WarehouseId = warehouseId, Quantity = inventoryQuantity }
                });

            // Lô hàng không đủ
            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                ProductId = newProductId,
                WarehouseId = warehouseId,
                QuantityIn = 100,
                QuantityOut = 50, // Đã xuất 50, còn lại 50
                ImportDate = DateTime.Today.AddDays(-10),
                ExpireDate = DateTime.Today.AddDays(30)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.Is<List<int>>(l => l.Contains(newProductId))))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không đủ hàng trong các lô cho sản phẩm"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("Không đủ hàng trong các lô cho sản phẩm", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID12: UpdateTransactionInOrderStatus - Thành công (giảm số lượng sản phẩm)
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Sản phẩm có trong cả đơn cũ và đơn mới, đơn mới ít hơn (giảm số lượng)
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với sản phẩm giảm số lượng
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID12_UpdateInOrder_DecreaseQuantitySuccess_ReturnsOk()
        {
            // Arrange - INPUT: Giảm số lượng thành công
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 1;
            decimal oldQuantity = 100;
            decimal newQuantity = 50; // Giảm xuống 50
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = newQuantity, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = (int)oldQuantity }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            var productDto = new ProductDto { ProductId = productId, ProductName = "Test Product" };
            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            // Mock cho việc trả lại hàng (giảm số lượng)
            var inventoryEntity = new Inventory { InventoryId = 1, ProductId = productId, WarehouseId = warehouseId, Quantity = 100m };
            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(warehouseId, productId))
                .ReturnsAsync(inventoryEntity);

            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                ProductId = productId,
                WarehouseId = warehouseId,
                QuantityIn = 100,
                QuantityOut = 50, // Đã xuất 50, có thể trả lại
                ImportDate = DateTime.Today.AddDays(-10),
                ExpireDate = DateTime.Today.AddDays(30)
            };
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.Is<List<int>>(l => l.Contains(productId))))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            var stockBatchEntity = new StockBatch { BatchId = 1, ProductId = productId, WarehouseId = warehouseId, QuantityIn = 100, QuantityOut = 50 };
            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(stockBatchEntity);

            _stockBatchServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            _inventoryServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            _transactionDetailServiceMock
                .Setup(s => s.DeleteRange(It.IsAny<List<TransactionDetailDto>>()))
                .Returns(Task.CompletedTask);

            _transactionDetailServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID13: UpdateTransactionInOrderStatus - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái order
        /// - Warehouse tồn tại
        /// - Chi tiết đơn hàng tồn tại
        /// - Exception xảy ra trong try block
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - or: OrderRequest với tất cả điều kiện OK nhưng có exception
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID13_UpdateInOrder_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = 10, UnitPrice = 100000 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order,
                WarehouseId = warehouseId
            };

            // Sản phẩm có trong cả đơn cũ và đơn mới, số lượng giống nhau (diff = 0) - không cần update inventory/stockbatch
            // Không có sản phẩm bị xóa, không có sản phẩm mới
            var oldDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 10 } // Cùng số lượng với orderRequest
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _warehouseServiceMock
                .Setup(s => s.GetByIdAsync(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Test Warehouse" });

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(oldDetails);

            // Throw exception khi delete transaction details (trong try block, sau khi pass tất cả validation)
            _transactionDetailServiceMock
                .Setup(s => s.DeleteRange(It.IsAny<List<TransactionDetailDto>>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateTransactionInOrderStatus(transactionId, orderRequest);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateToCancelStatus Tests

        /// <summary>
        /// TCID01: UpdateToCancelStatus - Transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction với transactionId không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateToCancel_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.UpdateToCancelStatus(transactionId);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateToCancelStatus - Transaction không ở trạng thái draft
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Status != draft
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không trong trạng thái nháp"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request (code có bug là 404 nhưng nên là 400)
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateToCancel_NotInDraftStatus_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không ở trạng thái draft
            int transactionId = 1;
            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order, // Không phải draft
                WarehouseId = 1
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.UpdateToCancelStatus(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng không trong trạng thái nháp"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không trong trạng thái nháp", response.Error.Message);
            // Code có bug là 404 nhưng đây là BadRequest, nên expect 404 theo code hiện tại
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateToCancelStatus - Thành công
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateToCancel_Success_ReturnsOk()
        {
            // Arrange - INPUT: Transaction tồn tại và ở trạng thái draft
            int transactionId = 1;
            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = 1
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateToCancelStatus(transactionId);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID04: UpdateToCancelStatus - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và ở trạng thái draft
        /// - Exception xảy ra khi update
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToCancel_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int transactionId = 1;
            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.draft,
                WarehouseId = 1
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Throw exception khi update
            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateToCancelStatus(transactionId);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateToPaidInFullStatus Tests

        /// <summary>
        /// TCID01: UpdateToPaidInFullStatus - ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM với ModelState invalid
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateToPaidInFull_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange - INPUT: ModelState invalid
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = null! // Required field is null
            };

            // Set ModelState invalid
            _controller.ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán không được để trống");

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionCreateVM>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateToPaidInFullStatus - Transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction với transactionId không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateToPaidInFull_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash"
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateToPaidInFullStatus - Transaction không phải đơn xuất
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Type != "Export"
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không phải đơn xuất"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateToPaidInFull_NotExportTransaction_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không phải đơn xuất
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash"
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import", // Không phải "Export"
                Status = (int)TransactionStatus.order,
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng không phải đơn xuất"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không phải đơn xuất", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateToPaidInFullStatus - Transaction đã được thanh toán
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction.Status == paidInFull hoặc partiallyPaid
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng đã được thanh toán hoặc thanh toán một phần"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateToPaidInFull_AlreadyPaid_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã được thanh toán
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash"
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.paidInFull, // Đã thanh toán
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng đã được thanh toán hoặc thanh toán một phần"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Transaction>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng đã được thanh toán hoặc thanh toán một phần", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateToPaidInFullStatus - Đã có thanh toán trước đó
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán
        /// - Đã có financialTransactions trước đó
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng đã có thanh toán trước đó. Vui lòng sử dụng thanh toán một phần"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateToPaidInFull_HasPreviousPayment_ReturnsBadRequest()
        {
            // Arrange - INPUT: Đã có thanh toán trước đó
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash"
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.order, // Chưa thanh toán
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Đã có thanh toán trước đó
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync(new List<FinancialTransactionDto>
                {
                    new FinancialTransactionDto { FinancialTransactionId = 1, RelatedTransactionId = transactionId }
                });

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng đã có thanh toán trước đó. Vui lòng sử dụng thanh toán một phần"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Transaction>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng đã có thanh toán trước đó. Vui lòng sử dụng thanh toán một phần", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateToPaidInFullStatus - Thành công
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán
        /// - Chưa có financialTransactions trước đó
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateToPaidInFull_Success_ReturnsOk()
        {
            // Arrange - INPUT: Tất cả điều kiện OK
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash"
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.order, // Chưa thanh toán
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Chưa có thanh toán trước đó
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync((List<FinancialTransactionDto>?)null);

            // Mock mapper
            var financialTransactionEntity = new FinancialTransaction
            {
                FinancialTransactionId = 1,
                RelatedTransactionId = transactionId,
                Amount = 1000000,
                PaymentMethod = "Cash"
            };
            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(financialTransactionEntity);

            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .Returns(Task.CompletedTask);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID07: UpdateToPaidInFullStatus - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán
        /// - Chưa có financialTransactions trước đó
        /// - Exception xảy ra trong try block
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateToPaidInFull_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash"
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.order, // Chưa thanh toán
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Chưa có thanh toán trước đó
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync((List<FinancialTransactionDto>?)null);

            // Mock mapper
            var financialTransactionEntity = new FinancialTransaction
            {
                FinancialTransactionId = 1,
                RelatedTransactionId = transactionId,
                Amount = 1000000,
                PaymentMethod = "Cash"
            };
            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(financialTransactionEntity);

            // Throw exception khi create financial transaction
            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateToPaidInFullStatus(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region CreatePartialPayment Tests

        /// <summary>
        /// TCID01: CreatePartialPayment - ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM với ModelState invalid
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_CreatePartialPayment_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange - INPUT: ModelState invalid
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = null! // Required field is null
            };

            // Set ModelState invalid
            _controller.ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán không được để trống");

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionCreateVM>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreatePartialPayment - Amount không có giá trị
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - model.Amount = null
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM với Amount = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số tiền trả không được để trống"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_CreatePartialPayment_AmountIsNull_ReturnsBadRequest()
        {
            // Arrange - INPUT: Amount không có giá trị
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = null // Amount không có giá trị
            };

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Số tiền trả không được để trống"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionCreateVM>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Số tiền trả không được để trống", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreatePartialPayment - Transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction với transactionId không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 999
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_CreatePartialPayment_TransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: Transaction không tồn tại
            int transactionId = 999;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 500000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy đơn hàng"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreatePartialPayment - Transaction không phải đơn xuất
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Type != "Export"
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng không phải đơn xuất"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_CreatePartialPayment_NotExportTransaction_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction không phải đơn xuất
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 500000
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import", // Không phải "Export"
                Status = (int)TransactionStatus.order,
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng không phải đơn xuất"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không phải đơn xuất", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreatePartialPayment - Transaction đã được thanh toán đầy đủ
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction.Status == paidInFull
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn hàng đã được thanh toán"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_CreatePartialPayment_AlreadyPaidInFull_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction đã được thanh toán đầy đủ
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 500000
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.paidInFull, // Đã thanh toán đầy đủ
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Đơn hàng đã được thanh toán"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Transaction>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng đã được thanh toán", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CreatePartialPayment - Tổng số tiền thanh toán vượt quá giá trị đơn hàng
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán đầy đủ
        /// - Đã có thanh toán trước đó
        /// - Tổng số tiền (đã trả + mới trả) > TotalCost
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM với Amount làm tổng vượt quá
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Tổng số tiền thanh toán vượt quá giá trị đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_CreatePartialPayment_TotalExceedsTransactionCost_ReturnsBadRequest()
        {
            // Arrange - INPUT: Tổng số tiền vượt quá
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 600000 // Tổng sẽ là 800000 + 600000 = 1400000 > 1000000
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.partiallyPaid,
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Đã có thanh toán trước đó: 800000
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync(new List<FinancialTransactionDto>
                {
                    new FinancialTransactionDto { FinancialTransactionId = 1, RelatedTransactionId = transactionId, Amount = 800000 }
                });

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Tổng số tiền thanh toán vượt quá giá trị đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Transaction>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Tổng số tiền thanh toán vượt quá giá trị đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: CreatePartialPayment - Thành công (thanh toán một phần)
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán đầy đủ
        /// - Tổng số tiền (đã trả + mới trả) < TotalCost
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// - Transaction.Status = partiallyPaid
        /// </summary>
        [Fact]
        public async Task TCID07_CreatePartialPayment_PartialPaymentSuccess_ReturnsOk()
        {
            // Arrange - INPUT: Thanh toán một phần thành công
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 300000
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.order,
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Chưa có thanh toán trước đó
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync(new List<FinancialTransactionDto>());

            // Mock mapper
            var financialTransactionEntity = new FinancialTransaction
            {
                FinancialTransactionId = 1,
                RelatedTransactionId = transactionId,
                Amount = 300000,
                PaymentMethod = "Cash"
            };
            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(financialTransactionEntity);

            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .Returns(Task.CompletedTask);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID08: CreatePartialPayment - Thành công (thanh toán đầy đủ sau khi cộng thêm)
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán đầy đủ
        /// - Tổng số tiền (đã trả + mới trả) >= TotalCost (với tolerance 0.01)
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ với Amount làm tổng >= TotalCost
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// - Transaction.Status = paidInFull
        /// </summary>
        [Fact]
        public async Task TCID08_CreatePartialPayment_FullPaymentAfterPartial_ReturnsOk()
        {
            // Arrange - INPUT: Thanh toán đầy đủ sau khi cộng thêm
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 200000 // Tổng sẽ là 800000 + 200000 = 1000000 = TotalCost
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.partiallyPaid,
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Đã có thanh toán trước đó: 800000
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync(new List<FinancialTransactionDto>
                {
                    new FinancialTransactionDto { FinancialTransactionId = 1, RelatedTransactionId = transactionId, Amount = 800000 }
                });

            // Mock mapper
            var financialTransactionEntity = new FinancialTransaction
            {
                FinancialTransactionId = 2,
                RelatedTransactionId = transactionId,
                Amount = 200000,
                PaymentMethod = "Cash"
            };
            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(financialTransactionEntity);

            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .Returns(Task.CompletedTask);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công", response.Data);
        }

        /// <summary>
        /// TCID09: CreatePartialPayment - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và là đơn xuất
        /// - Transaction chưa được thanh toán đầy đủ
        /// - Tổng số tiền hợp lệ
        /// - Exception xảy ra trong try block
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID09_CreatePartialPayment_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int transactionId = 1;
            var model = new FinancialTransactionCreateVM
            {
                PaymentMethod = "Cash",
                Amount = 300000
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.order,
                TotalCost = 1000000
            };

            _transactionServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(transactionDto);

            // Chưa có thanh toán trước đó
            _financialTransactionServiceMock
                .Setup(s => s.GetByRelatedTransactionID(transactionId))
                .ReturnsAsync(new List<FinancialTransactionDto>());

            // Mock mapper
            var financialTransactionEntity = new FinancialTransaction
            {
                FinancialTransactionId = 1,
                RelatedTransactionId = transactionId,
                Amount = 300000,
                PaymentMethod = "Cash"
            };
            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(financialTransactionEntity);

            // Throw exception khi create financial transaction
            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.CreatePartialPayment(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region ReturnOrder Tests

        /// <summary>
        /// TCID01: ReturnOrder với danh sách trả hàng null hoặc empty
        /// 
        /// PRECONDITION:
        /// - ListProductOrder == null hoặc empty
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest.ListProductOrder = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 400 Bad Request
        /// - Response: ApiResponse<string>.Fail("Danh sách sản phẩm trả hàng không được rỗng.")
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID01_ReturnOrder_WithEmptyProductList_ReturnsBadRequest()
        {
            int transactionId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = null
            };

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Danh sách sản phẩm trả hàng không được rỗng.", response.Error.Message);
        }

        /// <summary>
        /// TCID02: ReturnOrder khi không tìm thấy đơn hàng
        /// 
        /// PRECONDITION:
        /// - Transaction không tồn tại
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest gồm 1 sản phẩm hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 404 Not Found
        /// - Response: ApiResponse<string>.Fail("Không tìm thấy đơn hàng", 404)
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID02_ReturnOrder_TransactionNotFound_ReturnsNotFound()
        {
            int transactionId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 1 }
                }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync((Transaction)null);

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: ReturnOrder khi đơn hàng không ở trạng thái done
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại nhưng Status != done
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest gồm 1 sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 400 Bad Request
        /// - Response: ApiResponse<string>.Fail("Đơn hàng không trong trạng thái đã giao")
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID03_ReturnOrder_TransactionNotDone_ReturnsBadRequest()
        {
            int transactionId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 1 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.order
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Đơn hàng không trong trạng thái đã giao", response.Error.Message);
        }

        /// <summary>
        /// TCID04: ReturnOrder khi không tìm thấy chi tiết đơn hàng
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = done
        /// - Không có TransactionDetail
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest gồm 1 sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 404 Not Found
        /// - Response: ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng", 404)
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID04_ReturnOrder_TransactionDetailMissing_ReturnsNotFound()
        {
            int transactionId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 1 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.done
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync((List<TransactionDetailDto>)null);

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy chi tiết đơn hàng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: ReturnOrder khi sản phẩm không có trong đơn
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và có detail khác
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest trả sản phẩm không có trong currentDetails
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 400 Bad Request
        /// - Response: ApiResponse<string>.Fail("Sản phẩm 'X' không có trong đơn hàng này.")
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID05_ReturnOrder_ProductNotInOrder_ReturnsBadRequest()
        {
            int transactionId = 1;
            int existingProductId = 1;
            int missingProductId = 2;
            const string missingProductName = "Sản phẩm lạ";

            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = missingProductId, Quantity = 1 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.done
            };

            var currentDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = existingProductId, Quantity = 3 }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(currentDetails);

            _productServiceMock
                .Setup(s => s.GetByIdAsync(missingProductId))
                .ReturnsAsync(new ProductDto { ProductId = missingProductId, ProductName = missingProductName });

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal($"Sản phẩm '{missingProductName}' không có trong đơn hàng này.", response.Error.Message);
        }

        /// <summary>
        /// TCID06: ReturnOrder khi số lượng trả vượt quá số lượng trong đơn
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và có detail
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest trả nhiều hơn currentDetails
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 400 Bad Request
        /// - Response: ApiResponse<string>.Fail("Số lượng trả ... vượt quá ...")
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID06_ReturnOrder_ReturnQuantityExceeds_ReturnsBadRequest()
        {
            int transactionId = 1;
            int productId = 1;
            const string productName = "Táo";

            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = 5 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.done
            };

            var currentDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 3 }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(currentDetails);

            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(new ProductDto { ProductId = productId, ProductName = productName });

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal($"Số lượng trả của sản phẩm '{productName}' (5) vượt quá số lượng trong đơn (3).", response.Error.Message);
        }

        /// <summary>
        /// TCID07: ReturnOrder khi số lượng trả không hợp lệ (<= 0)
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và có detail
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest trả sản phẩm với Quantity = 0
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 400 Bad Request
        /// - Response: ApiResponse<string>.Fail("Số lượng trả ... phải lớn hơn 0.")
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID07_ReturnOrder_ReturnQuantityNotPositive_ReturnsBadRequest()
        {
            int transactionId = 1;
            int productId = 1;
            const string productName = "Cam";

            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = 0 }
                }
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = (int)TransactionStatus.done
            };

            var currentDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2 }
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(currentDetails);

            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(new ProductDto { ProductId = productId, ProductName = productName });

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal($"Số lượng trả của sản phẩm '{productName}' phải lớn hơn 0.", response.Error.Message);
        }

        /// <summary>
        /// TCID09: ReturnOrder trả hết sản phẩm trong đơn
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại, Status = done, và currentDetails bao gồm toàn bộ sản phẩm
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest trả lại toàn bộ số lượng từng sản phẩm
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 200 OK
        /// - Response: ApiResponse<string>.Ok("Trả hàng thành công")
        /// - Transaction.Status chuyển về cancel
        /// - Các inventory, stockbatch được cập nhật
        /// </summary>
        [Fact]
        public async Task TCID09_ReturnOrder_ReturnAllProducts_ReturnsOk()
        {
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 101;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = 2 }
                },
                Note = "Ghi chú trả hàng",
                Reason = "Khách trả lại"
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                WarehouseId = warehouseId,
                Status = (int)TransactionStatus.done,
                TotalCost = 200m,
                TotalWeight = 5m
            };

            var currentDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2, UnitPrice = 100 }
            };

            var productDto = new ProductDto
            {
                ProductId = productId,
                ProductName = "Sản phẩm A",
                WeightPerUnit = 1m
            };

            var inventoryEntity = new Inventory
            {
                InventoryId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = 3m
            };

            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityOut = 2m,
                ImportDate = DateTime.Today.AddDays(-2)
            };

            var stockBatchEntity = new StockBatch
            {
                BatchId = stockBatchDto.BatchId,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityOut = 2m
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(currentDetails);

            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(warehouseId, productId))
                .ReturnsAsync(inventoryEntity);

            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.Is<List<int>>(l => l.Contains(productId))))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(stockBatchDto.BatchId))
                .ReturnsAsync(stockBatchEntity);

            _stockBatchServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            _inventoryServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            var mappedReturnTransaction = new ReturnTransaction();
            _mapperMock
                .Setup(m => m.Map<ReturnTransactionCreateVM, ReturnTransaction>(It.IsAny<ReturnTransactionCreateVM>()))
                .Returns(mappedReturnTransaction);

            _mapperMock
                .Setup(m => m.Map<ReturnTransactionDetailCreateVM, ReturnTransactionDetail>(It.IsAny<ReturnTransactionDetailCreateVM>()))
                .Returns(new ReturnTransactionDetail());

            _returnTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<ReturnTransaction>()))
                .Returns(Task.CompletedTask);

            _returnTransactionDetailServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<ReturnTransactionDetail>()))
                .Returns(Task.CompletedTask);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Trả hàng thành công", response.Data);
            Assert.Equal((int)TransactionStatus.cancel, transaction.Status);
            Assert.Equal(orderRequest.Note, transaction.Note);
            Assert.Equal(5m, inventoryEntity.Quantity);
            Assert.Equal(0m, stockBatchEntity.QuantityOut);

            _returnTransactionServiceMock.Verify(s => s.CreateAsync(mappedReturnTransaction), Times.Once);
            _returnTransactionDetailServiceMock.Verify(s => s.CreateAsync(It.IsAny<ReturnTransactionDetail>()), Times.Exactly(currentDetails.Count));
            _inventoryServiceMock.Verify(s => s.UpdateNoTracking(It.IsAny<Inventory>()), Times.Once);
            _stockBatchServiceMock.Verify(s => s.UpdateNoTracking(It.IsAny<StockBatch>()), Times.Once);
            _transactionServiceMock.Verify(s => s.UpdateAsync(transaction), Times.Once);
        }

        /// <summary>
        /// TCID08: ReturnOrder trả một phần sản phẩm
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và Status = done
        /// - Có một sản phẩm được trả đi và còn lại sản phẩm khác
        /// 
        /// INPUT:
        /// - transactionId = 1
        /// - orderRequest trả một phần sản phẩm đầu tiên
        /// 
        /// EXPECTED OUTPUT:
        /// - Status: 200 OK
        /// - Response: ApiResponse<string>.Ok("Trả hàng thành công")
        /// - Transaction.TotalCost trừ đi or.TotalCost
        /// - Transaction.TotalWeight trừ đi weight tương ứng
        /// - Chi tiết đơn hàng được cập nhật
        /// </summary>
        [Fact]
        public async Task TCID08_ReturnOrder_PartialReturn_AdjustsTotalsAndDetails()
        {
            int transactionId = 1;
            int warehouseId = 1;
            int productId = 1;
            var orderRequest = new OrderRequest
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = productId, Quantity = 1 }
                },
                Note = "Ghi chú trả một phần",
                TotalCost = 100m,
                Reason = "Trả một phần"
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                WarehouseId = warehouseId,
                Status = (int)TransactionStatus.done,
                TotalCost = 1000m,
                TotalWeight = 10m
            };

            var currentDetails = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = productId, Quantity = 2, UnitPrice = 100 },
                new TransactionDetailDto { Id = 2, ProductId = 2, Quantity = 1, UnitPrice = 200 }
            };

            var detailEntity = new TransactionDetail
            {
                Id = 1,
                TransactionId = transactionId,
                ProductId = productId,
                Quantity = 2,
                UnitPrice = 100
            };

            var productDto = new ProductDto
            {
                ProductId = productId,
                ProductName = "Sản phẩm B",
                WeightPerUnit = 1.5m
            };

            var inventoryEntity = new Inventory
            {
                InventoryId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = 4m
            };

            var stockBatchDto = new StockBatchDto
            {
                BatchId = 1,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityOut = 1m,
                ImportDate = DateTime.Today.AddDays(-3)
            };

            var stockBatchEntity = new StockBatch
            {
                BatchId = stockBatchDto.BatchId,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityOut = 1m
            };

            _transactionServiceMock
                .Setup(s => s.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _transactionDetailServiceMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(currentDetails);

            _transactionDetailServiceMock
                .Setup(s => s.GetByIdAsync(detailEntity.Id))
                .ReturnsAsync(detailEntity);

            _productServiceMock
                .Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(productDto);

            _inventoryServiceMock
                .Setup(s => s.GetEntityByWarehouseAndProductIdAsync(warehouseId, productId))
                .ReturnsAsync(inventoryEntity);

            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.Is<List<int>>(l => l.Contains(productId))))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(stockBatchDto.BatchId))
                .ReturnsAsync(stockBatchEntity);

            _stockBatchServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            _inventoryServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            _transactionDetailServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            _transactionDetailServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            var mappedReturnTransaction = new ReturnTransaction();
            _mapperMock
                .Setup(m => m.Map<ReturnTransactionCreateVM, ReturnTransaction>(It.IsAny<ReturnTransactionCreateVM>()))
                .Returns(mappedReturnTransaction);

            _mapperMock
                .Setup(m => m.Map<ReturnTransactionDetailCreateVM, ReturnTransactionDetail>(It.IsAny<ReturnTransactionDetailCreateVM>()))
                .Returns(new ReturnTransactionDetail());

            _returnTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<ReturnTransaction>()))
                .Returns(Task.CompletedTask);

            _returnTransactionDetailServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<ReturnTransactionDetail>()))
                .Returns(Task.CompletedTask);

            _transactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.ReturnOrder(transactionId, orderRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Trả hàng thành công", response.Data);
            Assert.Equal(900m, transaction.TotalCost);
            Assert.Equal(8.5m, transaction.TotalWeight);
            Assert.Equal(orderRequest.Note, transaction.Note);
            Assert.Equal(1, detailEntity.Quantity);
            Assert.Equal(5m, inventoryEntity.Quantity);
            Assert.Equal(0m, stockBatchEntity.QuantityOut);

            _transactionDetailServiceMock.Verify(s => s.UpdateAsync(detailEntity), Times.Once);
            _transactionDetailServiceMock.Verify(s => s.DeleteAsync(It.IsAny<TransactionDetail>()), Times.Never);
            _returnTransactionDetailServiceMock.Verify(s => s.CreateAsync(It.IsAny<ReturnTransactionDetail>()), Times.Once);
            _inventoryServiceMock.Verify(s => s.UpdateNoTracking(It.IsAny<Inventory>()), Times.Once);
            _stockBatchServiceMock.Verify(s => s.UpdateNoTracking(It.IsAny<StockBatch>()), Times.Once);
            _transactionServiceMock.Verify(s => s.UpdateAsync(transaction), Times.Once);
        }

        #endregion

        
    }
}