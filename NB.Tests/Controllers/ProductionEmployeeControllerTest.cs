using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Dto;
using NB.Service.ProductionOrderService;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using Xunit;

namespace NB.Tests.Controllers
{
    public class ProductionEmployeeControllerTest
    {
        private readonly Mock<IProductionOrderService> _productionOrderServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<ProductionEmployeeController>> _loggerMock;
        private readonly ProductionEmployeeController _controller;

        public ProductionEmployeeControllerTest()
        {
            _productionOrderServiceMock = new Mock<IProductionOrderService>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<ProductionEmployeeController>>();

            _controller = new ProductionEmployeeController(
                _productionOrderServiceMock.Object,
                _userServiceMock.Object,
                _loggerMock.Object);

            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
        }

        private void SetUserClaim(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        #region GetMyProductionOrders Tests

        /// <summary>
        /// TCID01: GetMyProductionOrders khi không thể lấy user claim
        ///
        /// PRECONDITION:
        /// - User claim trống hoặc không hợp lệ
        ///
        /// INPUT:
        /// - search = null
        /// - User không có claim NameIdentifier
        ///
        /// EXPECTED OUTPUT:
        /// - Unauthorized với message "Không thể xác định người dùng"
        /// - Type: A (Abnormal)
        /// - Status: 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task TCID01_GetMyProductionOrders_NoUserClaim_ReturnsUnauthorized()
        {
            // Arrange - INPUT: search = null, user claim missing
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await _controller.GetMyProductionOrders(null!);

            // Assert - EXPECTED OUTPUT: Unauthorized message
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể xác định người dùng", response.Error.Message);
            Assert.Equal(401, response.StatusCode);
        }

        /// <summary>
        /// TCID02: GetMyProductionOrders khi user không tồn tại
        ///
        /// PRECONDITION:
        /// - User claim có giá trị hợp lệ
        /// - IUserService.GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - search = null
        /// - claim NameIdentifier = 5
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound với message "Không tìm thấy thông tin người dùng"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID02_GetMyProductionOrders_UserNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: valid claim nhưng user không tồn tại
            int userId = 5;
            SetUserClaim(userId);
            _userServiceMock
                .Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetMyProductionOrders(null!);

            // Assert - EXPECTED OUTPUT: NotFound message
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy thông tin người dùng", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: GetMyProductionOrders khi user tồn tại và có dữ liệu
        ///
        /// PRECONDITION:
        /// - User claim hợp lệ
        /// - UserService trả về user tồn tại
        /// - Service trả về danh sách lệnh sản xuất có status
        ///
        /// INPUT:
        /// - search = null (mặc định)
        /// - claim NameIdentifier = 7
        ///
        /// EXPECTED OUTPUT:
        /// - Ok với ApiResponse chứa PagedList có StatusName
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID03_GetMyProductionOrders_UserFound_ReturnsPagedList()
        {
            // Arrange - INPUT: user tồn tại, search null để vào nhánh default
            int userId = 7;
            SetUserClaim(userId);
            var existingUser = new UserDto { UserId = userId };
            _userServiceMock
                .Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            var items = new List<ProductionOrderDto>
            {
                new()
                {
                    Id = 123,
                    Status = (int)ProductionOrderStatus.Processing,
                    Note = "Test note",
                    CreatedAt = DateTime.UtcNow
                }
            };

            var pagedList = new PagedList<ProductionOrderDto>(items, pageIndex: 1, pageSize: 10, totalCount: items.Count);
            _productionOrderServiceMock
                .Setup(s => s.GetDataByResponsibleId(userId, It.IsAny<ProductionOrderSearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetMyProductionOrders(null!);

            // Assert - EXPECTED OUTPUT: Ok với ApiResponse thành công
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data!.Items);
            Assert.Equal(ProductionOrderStatus.Processing.GetDescription(), response.Data.Items[0].StatusName);
        }

        /// <summary>
        /// TCID04: GetMyProductionOrders khi service ném exception
        ///
        /// PRECONDITION:
        /// - User claim hợp lệ
        /// - Service bị lỗi và ném exception
        ///
        /// INPUT:
        /// - search = null
        /// - claim NameIdentifier = 9
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message chứa lỗi
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_GetMyProductionOrders_ServiceThrows_ReturnsBadRequest()
        {
            // Arrange - INPUT: user tồn tại nhưng service throw
            int userId = 9;
            SetUserClaim(userId);
            var existingUser = new UserDto { UserId = userId };
            _userServiceMock
                .Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _productionOrderServiceMock
                .Setup(s => s.GetDataByResponsibleId(userId, It.IsAny<ProductionOrderSearch>()))
                .ThrowsAsync(new Exception("service failure"));

            // Act
            var result = await _controller.GetMyProductionOrders(null!);

            // Assert - EXPECTED OUTPUT: BadRequest message
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu: service failure", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region ChangeToProcessing Tests

        /// <summary>
        /// TCID08: ChangeToProcessing khi id không hợp lệ
        ///
        /// PRECONDITION:
        /// - id <= 0
        ///
        /// INPUT:
        /// - id = 0
        /// - request với DeviceCode hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "Id không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_ChangeToProcessing_InvalidId_ReturnsBadRequest()
        {
            var request = new ChangeToProcessingRequest
            {
                DeviceCode = "DEV-01"
            };

            var result = await _controller.ChangeToProcessing(0, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: ChangeToProcessing khi DeviceCode thiếu
        ///
        /// PRECONDITION:
        /// - id hợp lệ
        /// - request null hoặc DeviceCode trắng
        ///
        /// INPUT:
        /// - id = 5
        /// - request = null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "DeviceCode là bắt buộc"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID09_ChangeToProcessing_MissingDeviceCode_ReturnsBadRequest()
        {
            var result = await _controller.ChangeToProcessing(5, null!);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("DeviceCode là bắt buộc", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID10: ChangeToProcessing khi thiếu user claim
        ///
        /// PRECONDITION:
        /// - User claim không có NameIdentifier
        ///
        /// INPUT:
        /// - id = 7
        /// - request định nghĩa DeviceCode
        ///
        /// EXPECTED OUTPUT:
        /// - Unauthorized với message "Không thể xác định người dùng"
        /// - Type: A (Abnormal)
        /// - Status: 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task TCID10_ChangeToProcessing_NoUserClaim_ReturnsUnauthorized()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            var request = new ChangeToProcessingRequest
            {
                DeviceCode = "DEV-02"
            };

            var result = await _controller.ChangeToProcessing(7, request);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể xác định người dùng", response.Error.Message);
            Assert.Equal(401, response.StatusCode);
        }

        /// <summary>
        /// TCID11: ChangeToProcessing khi service trả về lỗi
        ///
        /// PRECONDITION:
        /// - id hợp lệ
        /// - DeviceCode hợp lệ
        /// - User được xác thực
        /// - Service trả ApiResponse.Fail
        ///
        /// INPUT:
        /// - id = 9
        /// - request DeviceCode = "DEV-03"
        /// - claim NameIdentifier = 3
        ///
        /// EXPECTED OUTPUT:
        /// - ObjectResult với status code theo ApiResponse
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID11_ChangeToProcessing_ServiceFails_ReturnsStatusCode()
        {
            int userId = 3;
            SetUserClaim(userId);
            var request = new ChangeToProcessingRequest
            {
                DeviceCode = "DEV-03"
            };

            var serviceResponse = ApiResponse<object>.Fail("service failure", 500);
            _productionOrderServiceMock
                .Setup(s => s.ChangeToProcessingAsync(9, request, userId))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.ChangeToProcessing(9, request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("service failure", response.Error.Message);
        }

        /// <summary>
        /// TCID12: ChangeToProcessing thành công
        ///
        /// PRECONDITION:
        /// - id hợp lệ
        /// - DeviceCode hợp lệ
        /// - User được xác thực
        /// - Service trả ApiResponse.Ok
        ///
        /// INPUT:
        /// - id = 12
        /// - request DeviceCode = "DEV-04"
        /// - claim NameIdentifier = 4
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Success
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID12_ChangeToProcessing_Succeeds_ReturnsOk()
        {
            int userId = 4;
            SetUserClaim(userId);
            var request = new ChangeToProcessingRequest
            {
                DeviceCode = "DEV-04"
            };

            var serviceResponse = ApiResponse<object>.Ok(new { Message = "Done" });
            _productionOrderServiceMock
                .Setup(s => s.ChangeToProcessingAsync(12, request, userId))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.ChangeToProcessing(12, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
        }

        #endregion

        #region GetProductionOrdersByEmployee Tests

        /// <summary>
        /// TCID05: GetProductionOrdersByEmployee khi employee không tồn tại
        ///
        /// PRECONDITION:
        /// - IUserService.GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - employeeId = 11
        /// - search = null
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound với message "Không tìm thấy thông tin nhân viên"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID05_GetProductionOrdersByEmployee_EmployeeNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: employee không tồn tại
            int employeeId = 11;
            _userServiceMock
                .Setup(s => s.GetByIdAsync(employeeId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetProductionOrdersByEmployee(employeeId, null!);

            // Assert - EXPECTED OUTPUT: NotFound message
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy thông tin nhân viên", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID06: GetProductionOrdersByEmployee trả về dữ liệu hợp lệ
        ///
        /// PRECONDITION:
        /// - employee tồn tại
        /// - Service trả về PagedList các lệnh sản xuất
        ///
        /// INPUT:
        /// - employeeId = 13
        /// - search = null
        ///
        /// EXPECTED OUTPUT:
        /// - Ok với ApiResponse chứa PagedList có StatusName
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID06_GetProductionOrdersByEmployee_ReturnsPagedList()
        {
            // Arrange - INPUT: employee tồn tại và service trả dữ liệu
            int employeeId = 13;
            var employee = new UserDto { UserId = employeeId };
            _userServiceMock
                .Setup(s => s.GetByIdAsync(employeeId))
                .ReturnsAsync(employee);

            var items = new List<ProductionOrderDto>
            {
                new()
                {
                    Id = 22,
                    Status = (int)ProductionOrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var pagedList = new PagedList<ProductionOrderDto>(items, pageIndex: 1, pageSize: 10, totalCount: items.Count);
            _productionOrderServiceMock
                .Setup(s => s.GetDataByResponsibleId(employeeId, It.IsAny<ProductionOrderSearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetProductionOrdersByEmployee(employeeId, null!);

            // Assert - EXPECTED OUTPUT: Ok result with StatusName assigned
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data!.Items);
            Assert.Equal(ProductionOrderStatus.Pending.GetDescription(), response.Data.Items[0].StatusName);
        }

        /// <summary>
        /// TCID07: GetProductionOrdersByEmployee khi service throw
        ///
        /// PRECONDITION:
        /// - employee tồn tại
        /// - Service ném exception
        ///
        /// INPUT:
        /// - employeeId = 15
        /// - search = null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message chứa lỗi
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID07_GetProductionOrdersByEmployee_ServiceThrows_ReturnsBadRequest()
        {
            // Arrange - INPUT: employee tồn tại nhưng service throw
            int employeeId = 15;
            var employee = new UserDto { UserId = employeeId };
            _userServiceMock
                .Setup(s => s.GetByIdAsync(employeeId))
                .ReturnsAsync(employee);

            _productionOrderServiceMock
                .Setup(s => s.GetDataByResponsibleId(employeeId, It.IsAny<ProductionOrderSearch>()))
                .ThrowsAsync(new Exception("service failure"));

            // Act
            var result = await _controller.GetProductionOrdersByEmployee(employeeId, null!);

            // Assert - EXPECTED OUTPUT: BadRequest message
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu: service failure", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region SubmitForApproval Tests

        /// <summary>
        /// TCID13: SubmitForApproval khi id không hợp lệ
        ///
        /// PRECONDITION:
        /// - id <= 0
        ///
        /// INPUT:
        /// - id = 0
        /// - request chứa FinishProductQuantities
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "Id không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID13_SubmitForApproval_InvalidId_ReturnsBadRequest()
        {
            var request = new SubmitForApprovalRequest
            {
                FinishProductQuantities = new List<FinishProductQuantity>
                {
                    new() { ProductId = 1, Quantity = 3 }
                }
            };

            var result = await _controller.SubmitForApproval(0, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID14: SubmitForApproval khi thiếu claim NameIdentifier
        ///
        /// PRECONDITION:
        /// - Claim không có NameIdentifier
        ///
        /// INPUT:
        /// - id = 5
        /// - request hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - Unauthorized với message "Không thể xác định người dùng"
        /// - Type: A (Abnormal)
        /// - Status: 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task TCID14_SubmitForApproval_NoUserClaim_ReturnsUnauthorized()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            var request = new SubmitForApprovalRequest
            {
                FinishProductQuantities = new List<FinishProductQuantity>
                {
                    new() { ProductId = 2, Quantity = 10 }
                }
            };

            var result = await _controller.SubmitForApproval(5, request);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(unauthorized.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể xác định người dùng", response.Error.Message);
            Assert.Equal(401, response.StatusCode);
        }

        /// <summary>
        /// TCID15: SubmitForApproval khi service trả lỗi
        ///
        /// PRECONDITION:
        /// - id hợp lệ
        /// - request hợp lệ
        /// - claim NameIdentifier tồn tại
        /// - Service trả ApiResponse.Fail
        ///
        /// INPUT:
        /// - id = 6
        /// - request với 1 finish product
        /// - claim NameIdentifier = 8
        ///
        /// EXPECTED OUTPUT:
        /// - ObjectResult chứa status code theo ApiResponse
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TCID15_SubmitForApproval_ServiceFails_ReturnsStatusCode()
        {
            int userId = 8;
            SetUserClaim(userId);
            var request = new SubmitForApprovalRequest
            {
                FinishProductQuantities = new List<FinishProductQuantity>
                {
                    new() { ProductId = 3, Quantity = 5 }
                }
            };

            var serviceResponse = ApiResponse<object>.Fail("service failure", 422);
            _productionOrderServiceMock
                .Setup(s => s.SubmitForApprovalAsync(6, request, userId))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.SubmitForApproval(6, request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(422, objectResult.StatusCode);
            var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("service failure", response.Error.Message);
        }

        /// <summary>
        /// TCID16: SubmitForApproval thành công
        ///
        /// PRECONDITION:
        /// - id hợp lệ
        /// - request hợp lệ
        /// - claim NameIdentifier tồn tại
        /// - Service trả ApiResponse.Ok
        ///
        /// INPUT:
        /// - id = 10
        /// - request với DeviceCode tham số
        /// - claim NameIdentifier = 9
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Success
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID16_SubmitForApproval_Succeeds_ReturnsOk()
        {
            int userId = 9;
            SetUserClaim(userId);
            var request = new SubmitForApprovalRequest
            {
                FinishProductQuantities = new List<FinishProductQuantity>
                {
                    new() { ProductId = 4, Quantity = 12 }
                }
            };

            var serviceResponse = ApiResponse<object>.Ok(new { Message = "Pending Approval" });
            _productionOrderServiceMock
                .Setup(s => s.SubmitForApprovalAsync(10, request, userId))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.SubmitForApproval(10, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
        }

        /// <summary>
        /// TCID17: SubmitForApproval khi service ném exception
        ///
        /// PRECONDITION:
        /// - claim NameIdentifier tồn tại
        /// - Service throw exception
        ///
        /// INPUT:
        /// - id = 11
        /// - request hợp lệ
        /// - claim NameIdentifier = 11
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message chứa lỗi
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID17_SubmitForApproval_ServiceThrows_ReturnsBadRequest()
        {
            int userId = 11;
            SetUserClaim(userId);
            var request = new SubmitForApprovalRequest
            {
                FinishProductQuantities = new List<FinishProductQuantity>
                {
                    new() { ProductId = 5, Quantity = 8 }
                }
            };

            _productionOrderServiceMock
                .Setup(s => s.SubmitForApprovalAsync(11, request, userId))
                .ThrowsAsync(new Exception("service boom"));

            var result = await _controller.SubmitForApproval(11, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra: service boom", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion
    }
}
