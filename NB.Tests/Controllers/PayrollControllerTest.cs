using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Dto;
using NB.Service.PayrollService;
using NB.Service.PayrollService.Dto;
using Xunit;

namespace NB.Tests.Controllers
{
    public class PayrollControllerTest
    {
        private readonly Mock<IPayrollService> _payrollServiceMock;
        private readonly Mock<ILogger<PayrollController>> _loggerMock;
        private readonly PayrollController _controller;

        public PayrollControllerTest()
        {
            _payrollServiceMock = new Mock<IPayrollService>();
            _loggerMock = new Mock<ILogger<PayrollController>>();

            _controller = new PayrollController(
                _payrollServiceMock.Object,
                _loggerMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        private void SetUserClaim(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext!.User = new ClaimsPrincipal(identity);
        }

        #region GetPayrollOverview Tests

        /// <summary>
        /// TCID29: GetPayrollOverview với năm không hợp lệ
        ///
        /// PRECONDITION:
        /// - year < 2000
        ///
        /// INPUT:
        /// - year = 1999, month = 5
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Năm không hợp lệ")
        /// </summary>
        [Fact]
        public async Task TCID29_GetPayrollOverview_InvalidYear_ReturnsBadRequest()
        {
            var result = await _controller.GetPayrollOverview(1999, 5);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<PayrollOverviewDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Năm không hợp lệ", response.Error?.Message);
        }

        /// <summary>
        /// TCID30: GetPayrollOverview với tháng không hợp lệ
        ///
        /// PRECONDITION:
        /// - month < 1
        ///
        /// INPUT:
        /// - year = 2025, month = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Tháng không hợp lệ (1-12)")
        /// </summary>
        [Fact]
        public async Task TCID30_GetPayrollOverview_InvalidMonth_ReturnsBadRequest()
        {
            var result = await _controller.GetPayrollOverview(2025, 0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<PayrollOverviewDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Tháng không hợp lệ (1-12)", response.Error?.Message);
        }

        /// <summary>
        /// TCID31: GetPayrollOverview khi service throw
        ///
        /// PRECONDITION:
        /// - năm, tháng hợp lệ
        /// - _payrollService throw error
        ///
        /// INPUT:
        /// - year = 2025, month = 5
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(exception.Message)
        /// </summary>
        [Fact]
        public async Task TCID31_GetPayrollOverview_ServiceThrows_ReturnsBadRequest()
        {
            var year = 2025;
            var month = 5;
            _payrollServiceMock
                .Setup(s => s.GetPayrollOverviewAsync(year, month))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await _controller.GetPayrollOverview(year, month);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<PayrollOverviewDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("boom", response.Error?.Message);
        }

        /// <summary>
        /// TCID32: GetPayrollOverview thành công
        ///
        /// PRECONDITION:
        /// - năm, tháng hợp lệ
        /// - _payrollService trả về danh sách
        ///
        /// INPUT:
        /// - year = 2025, month = 5
        ///
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok(List&lt;PayrollOverviewDto&gt;)
        /// - Các trường dữ liệu hiển thị đúng
        /// </summary>
        [Fact]
        public async Task TCID32_GetPayrollOverview_Succeeds_ReturnsOk()
        {
            var year = 2025;
            var month = 5;
            var overview = new List<PayrollOverviewDto>
            {
                new()
                {
                    EmployeeId = 10,
                    EmployeeName = "Nhân viên A",
                    TotalAmount = 1000000,
                    Status = "Chưa tạo",
                },
                new()
                {
                    EmployeeId = 11,
                    EmployeeName = "Nhân viên B",
                    TotalAmount = 1200000,
                    Status = "Đã tạo",
                    PayrollId = 45
                }
            };

            _payrollServiceMock
                .Setup(s => s.GetPayrollOverviewAsync(year, month))
                .ReturnsAsync(overview);

            var result = await _controller.GetPayrollOverview(year, month);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<PayrollOverviewDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(2, response.Data?.Count);
            Assert.Equal("Nhân viên A", response.Data?[0].EmployeeName);
            Assert.Equal(45, response.Data?[1].PayrollId);
            _payrollServiceMock.Verify(s => s.GetPayrollOverviewAsync(year, month), Times.Once);
        }

        #endregion

        #region PayPayroll Tests

        /// <summary>
        /// TCID38: PayPayroll khi không có claim
        ///
        /// PRECONDITION:
        /// - User không có claim NameIdentifier
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Không xác minh được vai trò")
        /// </summary>
        [Fact]
        public async Task TCID38_PayPayroll_NoClaim_ReturnsBadRequest()
        {
            _controller.ControllerContext.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity());
            var dto = new PayPayrollDto { PayrollId = 3, PaymentMethod="TienMat"};

            var result = await _controller.PayPayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PayPayrollResponseDto>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Không xác minh được vai trò", response.Error?.Message);
        }

        /// <summary>
        /// TCID39: PayPayroll thành công
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService trả về PayPayrollResponseDto
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok(response)
        /// </summary>
        [Fact]
        public async Task TCID39_PayPayroll_Succeeds_ReturnsOk()
        {
            var dto = new PayPayrollDto { PayrollId = 4, PaymentMethod = "NganHang" };
            var responseDto = new PayPayrollResponseDto { EmployeeId = 2, EmployeeName="NV", PaidDate=DateTime.UtcNow, PaymentMethod="NganHang", TotalAmount=1000 };
            SetUserClaim(9);

            _payrollServiceMock
                .Setup(s => s.PayPayrollAsync(dto, 9))
                .ReturnsAsync(responseDto);

            var result = await _controller.PayPayroll(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PayPayrollResponseDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("NV", response.Data?.EmployeeName);
            _payrollServiceMock.Verify(s => s.PayPayrollAsync(dto, 9), Times.Once);
        }

        /// <summary>
        /// TCID40: PayPayroll khi service ném ArgumentException
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService throw ArgumentException
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(message)
        /// </summary>
        [Fact]
        public async Task TCID40_PayPayroll_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            var dto = new PayPayrollDto { PayrollId = 5, PaymentMethod = "TienMat" };
            SetUserClaim(10);

            _payrollServiceMock
                .Setup(s => s.PayPayrollAsync(dto, 10))
                .ThrowsAsync(new ArgumentException("invalid dto"));

            var result = await _controller.PayPayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PayPayrollResponseDto>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("invalid dto", response.Error?.Message);
        }

        /// <summary>
        /// TCID41: PayPayroll khi service ném InvalidOperationException
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService throw InvalidOperationException
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(message)
        /// </summary>
        [Fact]
        public async Task TCID41_PayPayroll_ServiceThrowsInvalidOperation_ReturnsBadRequest()
        {
            var dto = new PayPayrollDto { PayrollId = 6, PaymentMethod = "TienMat" };
            SetUserClaim(11);

            _payrollServiceMock
                .Setup(s => s.PayPayrollAsync(dto, 11))
                .ThrowsAsync(new InvalidOperationException("already paid"));

            var result = await _controller.PayPayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PayPayrollResponseDto>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("already paid", response.Error?.Message);
        }

        /// <summary>
        /// TCID42: PayPayroll khi service ném exception tổng quát
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService throw Exception
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(message)
        /// </summary>
        [Fact]
        public async Task TCID42_PayPayroll_ServiceThrowsGeneralException_ReturnsBadRequest()
        {
            var dto = new PayPayrollDto { PayrollId = 7, PaymentMethod = "TienMat" };
            SetUserClaim(12);

            _payrollServiceMock
                .Setup(s => s.PayPayrollAsync(dto, 12))
                .ThrowsAsync(new Exception("unexpected"));

            var result = await _controller.PayPayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PayPayrollResponseDto>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("unexpected", response.Error?.Message);
        }

        #endregion

        #region CreatePayroll Tests

        /// <summary>
        /// TCID33: CreatePayroll khi không có claim
        ///
        /// PRECONDITION:
        /// - User không có claim NameIdentifier
        ///
        /// INPUT:
        /// - dto có EmployeeId, Year, Month hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Không xác minh được vai trò")
        /// </summary>
        [Fact]
        public async Task TCID33_CreatePayroll_NoClaim_ReturnsBadRequest()
        {
            _controller.ControllerContext.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity());
            var dto = new CreatePayrollDto { EmployeeId = 1, Year = 2025, Month = 5 };

            var result = await _controller.CreatePayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Payroll>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Không xác minh được vai trò", response.Error?.Message);
        }

        /// <summary>
        /// TCID34: CreatePayroll thành công
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService tạo được payroll
        ///
        /// INPUT:
        /// - dto hợp lệ + userId = 7
        ///
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok(payroll)
        /// </summary>
        [Fact]
        public async Task TCID34_CreatePayroll_Succeeds_ReturnsOk()
        {
            var dto = new CreatePayrollDto { EmployeeId = 1, Year = 2025, Month = 5 };
            var payroll = new Payroll { PayrollId = 9, EmployeeId = dto.EmployeeId };
            SetUserClaim(7);

            _payrollServiceMock
                .Setup(s => s.CreatePayrollAsync(dto, 7))
                .ReturnsAsync(payroll);

            var result = await _controller.CreatePayroll(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Payroll>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(9, response.Data?.PayrollId);
            _payrollServiceMock.Verify(s => s.CreatePayrollAsync(dto, 7), Times.Once);
        }

        /// <summary>
        /// TCID35: CreatePayroll khi service ném ArgumentException
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService throw ArgumentException
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(message)
        /// </summary>
        [Fact]
        public async Task TCID35_CreatePayroll_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            var dto = new CreatePayrollDto { EmployeeId = 1, Year = 2025, Month = 5 };
            SetUserClaim(3);

            _payrollServiceMock
                .Setup(s => s.CreatePayrollAsync(dto, 3))
                .ThrowsAsync(new ArgumentException("invalid dto"));

            var result = await _controller.CreatePayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Payroll>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("invalid dto", response.Error?.Message);
        }

        /// <summary>
        /// TCID36: CreatePayroll khi service ném InvalidOperationException
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService throw InvalidOperationException
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(message)
        /// </summary>
        [Fact]
        public async Task TCID36_CreatePayroll_ServiceThrowsInvalidOperation_ReturnsBadRequest()
        {
            var dto = new CreatePayrollDto { EmployeeId = 1, Year = 2025, Month = 5 };
            SetUserClaim(4);

            _payrollServiceMock
                .Setup(s => s.CreatePayrollAsync(dto, 4))
                .ThrowsAsync(new InvalidOperationException("no data"));

            var result = await _controller.CreatePayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Payroll>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("no data", response.Error?.Message);
        }

        /// <summary>
        /// TCID37: CreatePayroll khi service ném exception tổng quát
        ///
        /// PRECONDITION:
        /// - Claim hợp lệ
        /// - _payrollService throw Exception
        ///
        /// INPUT:
        /// - dto hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail(message)
        /// </summary>
        [Fact]
        public async Task TCID37_CreatePayroll_ServiceThrowsGeneralException_ReturnsBadRequest()
        {
            var dto = new CreatePayrollDto { EmployeeId = 1, Year = 2025, Month = 5 };
            SetUserClaim(5);

            _payrollServiceMock
                .Setup(s => s.CreatePayrollAsync(dto, 5))
                .ThrowsAsync(new Exception("unexpected"));

            var result = await _controller.CreatePayroll(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Payroll>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("unexpected", response.Error?.Message);
        }

        #endregion
    }
}
