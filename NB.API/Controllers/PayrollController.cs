using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.PayrollService;
using NB.Service.PayrollService.Dto;
using System.Security.Claims;

namespace NB.API.Controllers
{
    [Route("api/payroll")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;
        private readonly ILogger<PayrollController> _logger;

        public PayrollController(
            IPayrollService payrollService,
            ILogger<PayrollController> logger)
        {
            _payrollService = payrollService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tổng quan bảng lương theo tháng
        /// - Tự động tính từ WorkLog (chỉ lấy IsActive = true)
        /// - Tự động đồng bộ Payroll nếu đã tồn tại
        /// - Trả về danh sách nhân viên với tổng công, tổng tiền và trạng thái
        /// </summary>
        /// <param name="year">Năm (VD: 2025)</param>
        /// <param name="month">Tháng (VD: 11)</param>
        [HttpGet("overview")]
        public async Task<IActionResult> GetPayrollOverview(
            [FromQuery] int year,
            [FromQuery] int month)
        {
            try
            {
                if (year < 2000 || year > 2100)
                {
                    return BadRequest(ApiResponse<List<PayrollOverviewDto>>.Fail("Năm không hợp lệ"));
                }

                if (month < 1 || month > 12)
                {
                    return BadRequest(ApiResponse<List<PayrollOverviewDto>>.Fail("Tháng không hợp lệ (1-12)"));
                }

                var overview = await _payrollService.GetPayrollOverviewAsync(year, month);
                return Ok(ApiResponse<List<PayrollOverviewDto>>.Ok(overview));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tổng quan bảng lương tháng {Month}/{Year}", month, year);
                return BadRequest(ApiResponse<List<PayrollOverviewDto>>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Tạo bảng lương cho nhân viên trong tháng
        /// - Chỉ tạo được nếu chưa tồn tại bảng lương trong tháng đó
        /// - Tự động tính TotalAmount từ WorkLog (IsActive = true)
        /// - IsPaid mặc định = false
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayroll([FromBody] CreatePayrollDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdStr == null)
                    return BadRequest(ApiResponse<Payroll>.Fail("Không xác minh được vai trò"));

                var userId = int.Parse(userIdStr);
                var payroll = await _payrollService.CreatePayrollAsync(dto, userId);
                return Ok(ApiResponse<Payroll>.Ok(payroll));
            }
            catch (ArgumentException ex) //Lỗi do dữ liệu đầu vào không hợp lệ hoặc logic nghiệp vụ ném lỗi.
            {
                return BadRequest(ApiResponse<Payroll>.Fail(ex.Message));
            }
            catch (InvalidOperationException ex) //Lỗi này xảy ra khi code gọi một hàm nhưng không có dữ liệu phù hợp
            {
                return BadRequest(ApiResponse<Payroll>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bảng lương");
                return BadRequest(ApiResponse<Payroll>.Fail(ex.Message));
            }
        }
    }
}
