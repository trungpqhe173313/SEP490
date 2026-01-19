// csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinancialTransactionService;
using NB.Service.FinancialTransactionService.Dto;
using NB.Service.FinancialTransactionService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.PayrollService;
using NB.Service.SupplierService;
using Xunit;

namespace NB.Tests.Controllers
{
    public class FinancialTransactionControllerTest
    {
        // Mock dependencies - Các đối tượng giả lập để test
        private readonly Mock<ITransactionService> _transactionServiceMock;
        private readonly Mock<IFinancialTransactionService> _financialTransactionServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IPayrollService> _payrollServiceMock;
        private readonly Mock<ISupplierService> _supplierServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<FinancialTransactionController>> _loggerMock;
        private readonly FinancialTransactionController _controller;

        public FinancialTransactionControllerTest()
        {
            // Khởi tạo các mock objects
            _transactionServiceMock = new Mock<ITransactionService>();
            _financialTransactionServiceMock = new Mock<IFinancialTransactionService>();
            _userServiceMock = new Mock<IUserService>();
            _payrollServiceMock = new Mock<IPayrollService>();
            _supplierServiceMock = new Mock<ISupplierService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<FinancialTransactionController>>();

            // Khởi tạo controller với các dependencies đã mock
            _controller = new FinancialTransactionController(
                _transactionServiceMock.Object,
                _financialTransactionServiceMock.Object,
                _userServiceMock.Object,
                _payrollServiceMock.Object,
                _supplierServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: GetData - PageIndex và PageSize null
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false (do PageIndex/PageSize không hợp lệ)
        /// 
        /// INPUT:
        /// - search: FinancialTransactionSearch với PageIndex = null, PageSize = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_GetData_PageIndexAndPageSizeNull_ReturnsBadRequest()
        {
            // Arrange - INPUT: PageIndex và PageSize null (ModelState invalid)
            var search = new FinancialTransactionSearch();

            // Thêm lỗi vào ModelState để làm cho nó invalid (giả lập PageIndex/PageSize null không hợp lệ)
            _controller.ModelState.AddModelError("PageIndex", "PageIndex is required");
            _controller.ModelState.AddModelError("PageSize", "PageSize is required");

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: GetData - Vẫn tồn tại các financial transaction trước đó
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Vẫn tồn tại các financial transaction trước đó
        /// 
        /// INPUT:
        /// - search: FinancialTransactionSearch với PageIndex = 1, PageSize = 10
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với Status 200
        /// - result.Items != null và có danh sách dữ liệu
        /// - Type: N (Normal)
        /// </summary>
        [Fact]
        public async Task TCID02_GetData_FinancialTransactionsExist_ReturnsOk()
        {
            // Arrange - INPUT: Vẫn tồn tại các financial transaction trước đó
            var search = new FinancialTransactionSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Trả về danh sách có dữ liệu
            var financialTransactions = new List<FinancialTransactionDto>
            {
                new FinancialTransactionDto
                {
                    FinancialTransactionId = 1,
                    Amount = 1000000,
                    Type = FinancialTransactionType.ThuKhac.ToString(),
                    Description = "Thu khác",
                    PaymentMethod = "Tiền mặt",
                    TransactionDate = DateTime.Now,
                    CreatedBy = null
                },
                new FinancialTransactionDto
                {
                    FinancialTransactionId = 2,
                    Amount = -500000,
                    Type = FinancialTransactionType.ChiKhac.ToString(),
                    Description = "Chi khác",
                    PaymentMethod = "Chuyển khoản",
                    TransactionDate = DateTime.Now,
                    CreatedBy = null
                }
            };

            var pagedList = new PagedList<FinancialTransactionDto>(
                financialTransactions,
                search.PageIndex,
                search.PageSize,
                financialTransactions.Count
            );

            _financialTransactionServiceMock
                .Setup(s => s.GetData(search))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: Ok với Status 200, result.Items != null và có dữ liệu
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<FinancialTransactionDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Items); // Items != null
            Assert.NotEmpty(response.Data.Items); // Có danh sách dữ liệu
            Assert.Equal(2, response.Data.Items.Count);
            Assert.Equal(200, response.StatusCode);
        }

        /// <summary>
        /// TCID03: GetData - Không có financial transaction nào tồn tại trước đó
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Không có financial transaction nào tồn tại trước đó
        /// 
        /// INPUT:
        /// - search: FinancialTransactionSearch với PageIndex = 1, PageSize = 10
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với Status 200
        /// - result.Items = null
        /// - Type: N (Normal)
        /// </summary>
        [Fact]
        public async Task TCID03_GetData_NoFinancialTransactionExists_ItemsIsNull_ReturnsOk()
        {
            // Arrange - INPUT: Không có financial transaction nào tồn tại trước đó
            var search = new FinancialTransactionSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Clear ModelState để đảm bảo ModelState.IsValid = true (tránh ảnh hưởng từ test case trước)
            _controller.ModelState.Clear();

            // Tạo PagedList với Items = null (dùng reflection để set null vì constructor không cho phép)
            var pagedList = new PagedList<FinancialTransactionDto>(
                new List<FinancialTransactionDto>(), // Tạm thời tạo với empty list
                search.PageIndex,
                search.PageSize,
                0
            );

            // Dùng reflection để set Items = null
            var itemsProperty = typeof(PagedList<FinancialTransactionDto>).GetProperty("Items");
            itemsProperty?.SetValue(pagedList, null);

            _financialTransactionServiceMock
                .Setup(s => s.GetData(search))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: Ok với Status 200, result.Items = null
            // Note: Controller sẽ throw NullReferenceException ở dòng 101, nhưng theo template vẫn expect Ok
            // Trong thực tế, nếu Items = null thì sẽ rơi vào catch block và trả về BadRequest
            // Nhưng theo template yêu cầu, test case này expect Ok với Items = null
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<FinancialTransactionDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Null(response.Data.Items); // Items = null
            Assert.Equal(200, response.StatusCode);
        }

        /// <summary>
        /// TCID04: GetData - Có lỗi kết nối với Database
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Có lỗi kết nối với Database (exception xảy ra)
        /// 
        /// INPUT:
        /// - search: FinancialTransactionSearch với PageIndex = 1, PageSize = 10
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi lấy dữ liệu"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_GetData_DatabaseConnectionError_ReturnsBadRequest()
        {
            // Arrange - INPUT: Có lỗi kết nối với Database
            var search = new FinancialTransactionSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Giả lập lỗi kết nối Database
            _financialTransactionServiceMock
                .Setup(s => s.GetData(search))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi lấy dữ liệu"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<FinancialTransactionDto>>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region GetDetail Tests

        /// <summary>
        /// TCID01: GetDetail - ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false
        /// 
        /// INPUT:
        /// - Id: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_GetDetail_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange - INPUT: ModelState không hợp lệ
            int id = 1;

            // Thêm lỗi vào ModelState để làm cho nó invalid
            _controller.ModelState.AddModelError("TestError", "Test error message");

            // Act
            var result = await _controller.GetDetail(id);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: GetDetail - Id <= 0
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id <= 0
        /// 
        /// INPUT:
        /// - Id: 0
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Id không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_GetDetail_InvalidId_ReturnsBadRequest()
        {
            // Arrange - INPUT: Id <= 0
            int id = 0;

            // Act
            var result = await _controller.GetDetail(id);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Id không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionDto>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: GetDetail - FinancialTransaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - FinancialTransaction với Id này không tồn tại
        /// 
        /// INPUT:
        /// - Id: 999
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy giao dịch tài chính"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_GetDetail_FinancialTransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: FinancialTransaction không tồn tại
            int id = 999;

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync((FinancialTransactionDto?)null);

            // Act
            var result = await _controller.GetDetail(id);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy giao dịch tài chính"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionDto>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy giao dịch tài chính", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: GetDetail - Thành công
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - FinancialTransaction tồn tại
        /// 
        /// INPUT:
        /// - Id: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với FinancialTransactionDto
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID04_GetDetail_Success_ReturnsOk()
        {
            // Arrange - INPUT: Dữ liệu hợp lệ
            int id = 1;

            var financialTransaction = new FinancialTransactionDto
            {
                FinancialTransactionId = id,
                Amount = 1000000,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                Description = "Thu khác",
                PaymentMethod = "Tiền mặt",
                TransactionDate = DateTime.Now
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(financialTransaction);

            // Act
            var result = await _controller.GetDetail(id);

            // Assert - EXPECTED OUTPUT: Ok với FinancialTransactionDto
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(id, response.Data.FinancialTransactionId);
            Assert.Equal(200, response.StatusCode);
        }

        /// <summary>
        /// TCID05: GetDetail - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - Exception xảy ra khi gọi service
        /// 
        /// INPUT:
        /// - Id: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi lấy dữ liệu"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_GetDetail_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int id = 1;

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.GetDetail(id);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi lấy dữ liệu"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionDto>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region CreateFinancialTransaction Tests

        /// <summary>
        /// TCID01: CreateFinancialTransaction - ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false
        /// 
        /// INPUT:
        /// - model: FinancialTransactionCreateVM với ModelState invalid
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_CreateFinancialTransaction_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange - INPUT: ModelState không hợp lệ
            var model = new FinancialTransactionCreateVM();

            // Thêm lỗi vào ModelState để làm cho nó invalid
            _controller.ModelState.AddModelError("TestError", "Test error message");

            // Act
            var result = await _controller.CreateFinancialTransaction(model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionCreateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateFinancialTransaction - Amount không có giá trị
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Amount = null
        /// 
        /// INPUT:
        /// - model: FinancialTransactionCreateVM với Amount = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số tiền thu chi không được để trống"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_CreateFinancialTransaction_AmountIsNull_ReturnsBadRequest()
        {
            // Arrange - INPUT: Amount không có giá trị
            var model = new FinancialTransactionCreateVM
            {
                Amount = null,
                Type = (int)FinancialTransactionType.ThuKhac,
                PaymentMethod = "Tiền mặt"
            };

            // Act
            var result = await _controller.CreateFinancialTransaction(model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Số tiền thu chi không được để trống"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionCreateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Số tiền thu chi không được để trống", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreateFinancialTransaction - Type không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Amount có giá trị
        /// - Type không phải ThuKhac hoặc ChiKhac
        /// 
        /// INPUT:
        /// - model: FinancialTransactionCreateVM với Type không hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Kiểu thu chi không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_CreateFinancialTransaction_InvalidType_ReturnsBadRequest()
        {
            // Arrange - INPUT: Type không hợp lệ
            var model = new FinancialTransactionCreateVM
            {
                Amount = 1000000,
                Type = (int)FinancialTransactionType.ThuTienKhach, // Type không hợp lệ (chỉ cho phép ThuKhac hoặc ChiKhac)
                PaymentMethod = "Tiền mặt"
            };

            // Act
            var result = await _controller.CreateFinancialTransaction(model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Kiểu thu chi không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionCreateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Kiểu thu chi không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreateFinancialTransaction - Thành công
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Amount có giá trị
        /// - Type = ThuKhac hoặc ChiKhac
        /// 
        /// INPUT:
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Tạo khoản thu chi thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID04_CreateFinancialTransaction_Success_ReturnsOk()
        {
            // Arrange - INPUT: Dữ liệu hợp lệ
            var model = new FinancialTransactionCreateVM
            {
                Amount = 1000000,
                Type = (int)FinancialTransactionType.ThuKhac,
                PaymentMethod = "Tiền mặt",
                Description = "Thu khác"
            };

            var entity = new FinancialTransaction
            {
                FinancialTransactionId = 1,
                Amount = 1000000,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                PaymentMethod = "Tiền mặt",
                Description = "Thu khác",
                TransactionDate = DateTime.Now
            };

            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(entity);

            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateFinancialTransaction(model);

            // Assert - EXPECTED OUTPUT: Ok với message "Tạo khoản thu chi thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Tạo khoản thu chi thành công", response.Data);
            Assert.Equal(200, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreateFinancialTransaction - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Amount có giá trị
        /// - Type = ThuKhac hoặc ChiKhac
        /// - Exception xảy ra khi create
        /// 
        /// INPUT:
        /// - model: FinancialTransactionCreateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi tạo khoản thu chi"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_CreateFinancialTransaction_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            var model = new FinancialTransactionCreateVM
            {
                Amount = 1000000,
                Type = (int)FinancialTransactionType.ThuKhac,
                PaymentMethod = "Tiền mặt"
            };

            var entity = new FinancialTransaction
            {
                FinancialTransactionId = 1,
                Amount = 1000000,
                Type = FinancialTransactionType.ThuKhac.ToString()
            };

            _mapperMock
                .Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(model))
                .Returns(entity);

            _financialTransactionServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<FinancialTransaction>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.CreateFinancialTransaction(model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi tạo khoản thu chi"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi tạo khoản thu chi", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateFinancialTransaction Tests

        /// <summary>
        /// TCID01: UpdateFinancialTransaction - ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false
        /// 
        /// INPUT:
        /// - Id: 1
        /// - model: FinancialTransactionUpdateVM với ModelState invalid
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateFinancialTransaction_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange - INPUT: ModelState không hợp lệ
            int id = 1;
            var model = new FinancialTransactionUpdateVM();

            // Thêm lỗi vào ModelState để làm cho nó invalid
            _controller.ModelState.AddModelError("TestError", "Test error message");

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionUpdateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateFinancialTransaction - Id <= 0
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id <= 0
        /// 
        /// INPUT:
        /// - Id: 0
        /// - model: FinancialTransactionUpdateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Id không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateFinancialTransaction_InvalidId_ReturnsBadRequest()
        {
            // Arrange - INPUT: Id <= 0
            int id = 0;
            var model = new FinancialTransactionUpdateVM
            {
                Amount = 1000000
            };

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Id không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionUpdateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateFinancialTransaction - Amount không có giá trị
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - Amount = null
        /// 
        /// INPUT:
        /// - Id: 1
        /// - model: FinancialTransactionUpdateVM với Amount = null
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số tiền thu chi không được để trống"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateFinancialTransaction_AmountIsNull_ReturnsBadRequest()
        {
            // Arrange - INPUT: Amount không có giá trị
            int id = 1;
            var model = new FinancialTransactionUpdateVM
            {
                Amount = null
            };

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Số tiền thu chi không được để trống"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionUpdateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Số tiền thu chi không được để trống", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateFinancialTransaction - FinancialTransaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - Amount có giá trị
        /// - FinancialTransaction với Id này không tồn tại
        /// 
        /// INPUT:
        /// - Id: 999
        /// - model: FinancialTransactionUpdateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy giao dịch tài chính"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateFinancialTransaction_FinancialTransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: FinancialTransaction không tồn tại
            int id = 999;
            var model = new FinancialTransactionUpdateVM
            {
                Amount = 1000000
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync((FinancialTransactionDto?)null);

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy giao dịch tài chính"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionUpdateVM>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy giao dịch tài chính", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateFinancialTransaction - Type không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - Amount có giá trị
        /// - FinancialTransaction tồn tại
        /// - Type không hợp lệ (không phải một trong các giá trị cho phép)
        /// 
        /// INPUT:
        /// - Id: 1
        /// - model: FinancialTransactionUpdateVM với Type không hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Kiểu thu chi không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateFinancialTransaction_InvalidType_ReturnsBadRequest()
        {
            // Arrange - INPUT: Type không hợp lệ
            int id = 1;
            var model = new FinancialTransactionUpdateVM
            {
                Amount = 1000000,
                Type = 999 // Type không hợp lệ
            };

            var existingEntity = new FinancialTransactionDto
            {
                FinancialTransactionId = id,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                Amount = 1000000
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Kiểu thu chi không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FinancialTransactionUpdateVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Kiểu thu chi không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateFinancialTransaction - Thành công
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - Amount có giá trị
        /// - FinancialTransaction tồn tại
        /// - Type hợp lệ (hoặc null)
        /// 
        /// INPUT:
        /// - Id: 1
        /// - model: FinancialTransactionUpdateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật giao dịch tài chính thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateFinancialTransaction_Success_ReturnsOk()
        {
            // Arrange - INPUT: Dữ liệu hợp lệ
            int id = 1;
            var model = new FinancialTransactionUpdateVM
            {
                Amount = 1000000,
                Type = (int)FinancialTransactionType.ThuKhac,
                Description = "Thu khác",
                PaymentMethod = "Tiền mặt"
            };

            var existingEntity = new FinancialTransactionDto
            {
                FinancialTransactionId = id,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                Amount = 500000
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);

            _financialTransactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<FinancialTransactionDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật giao dịch tài chính thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật giao dịch tài chính thành công", response.Data);
            Assert.Equal(200, response.StatusCode);
        }

        /// <summary>
        /// TCID07: UpdateFinancialTransaction - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - Amount có giá trị
        /// - FinancialTransaction tồn tại
        /// - Exception xảy ra khi update
        /// 
        /// INPUT:
        /// - Id: 1
        /// - model: FinancialTransactionUpdateVM hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật giao dịch tài chính"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateFinancialTransaction_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int id = 1;
            var model = new FinancialTransactionUpdateVM
            {
                Amount = 1000000,
                Type = (int)FinancialTransactionType.ThuKhac
            };

            var existingEntity = new FinancialTransactionDto
            {
                FinancialTransactionId = id,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                Amount = 500000
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);

            _financialTransactionServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<FinancialTransactionDto>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.UpdateFinancialTransaction(id, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật giao dịch tài chính"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật giao dịch tài chính", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region DeleteFinancialTransaction Tests

        /// <summary>
        /// TCID01: DeleteFinancialTransaction - ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = false
        /// 
        /// INPUT:
        /// - Id: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Dữ liệu không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_DeleteFinancialTransaction_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange - INPUT: ModelState không hợp lệ
            int id = 1;

            // Thêm lỗi vào ModelState để làm cho nó invalid
            _controller.ModelState.AddModelError("TestError", "Test error message");

            // Act
            var result = await _controller.DeleteFinancialTransaction(id);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Dữ liệu không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: DeleteFinancialTransaction - Id <= 0
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id <= 0
        /// 
        /// INPUT:
        /// - Id: 0
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Id không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_DeleteFinancialTransaction_InvalidId_ReturnsBadRequest()
        {
            // Arrange - INPUT: Id <= 0
            int id = 0;

            // Act
            var result = await _controller.DeleteFinancialTransaction(id);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Id không hợp lệ"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: DeleteFinancialTransaction - FinancialTransaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - FinancialTransaction với Id này không tồn tại
        /// 
        /// INPUT:
        /// - Id: 999
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy giao dịch tài chính"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_DeleteFinancialTransaction_FinancialTransactionNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: FinancialTransaction không tồn tại
            int id = 999;

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync((FinancialTransactionDto?)null);

            // Act
            var result = await _controller.DeleteFinancialTransaction(id);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy giao dịch tài chính"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy giao dịch tài chính", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: DeleteFinancialTransaction - Thành công
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - FinancialTransaction tồn tại
        /// 
        /// INPUT:
        /// - Id: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Xóa giao dịch tài chính thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID04_DeleteFinancialTransaction_Success_ReturnsOk()
        {
            // Arrange - INPUT: Dữ liệu hợp lệ
            int id = 1;

            var existingEntity = new FinancialTransactionDto
            {
                FinancialTransactionId = id,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                Amount = 1000000
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);

            _financialTransactionServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<FinancialTransactionDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteFinancialTransaction(id);

            // Assert - EXPECTED OUTPUT: Ok với message "Xóa giao dịch tài chính thành công"
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Xóa giao dịch tài chính thành công", response.Data);
            Assert.Equal(200, response.StatusCode);
        }

        /// <summary>
        /// TCID05: DeleteFinancialTransaction - Exception xảy ra
        /// 
        /// PRECONDITION:
        /// - ModelState.IsValid = true
        /// - Id > 0
        /// - FinancialTransaction tồn tại
        /// - Exception xảy ra khi delete
        /// 
        /// INPUT:
        /// - Id: 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi xóa giao dịch tài chính"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_DeleteFinancialTransaction_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Exception sẽ xảy ra
            int id = 1;

            var existingEntity = new FinancialTransactionDto
            {
                FinancialTransactionId = id,
                Type = FinancialTransactionType.ThuKhac.ToString(),
                Amount = 1000000
            };

            _financialTransactionServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);

            _financialTransactionServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<FinancialTransactionDto>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.DeleteFinancialTransaction(id);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi xóa giao dịch tài chính"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi xóa giao dịch tài chính", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion
    }
}

