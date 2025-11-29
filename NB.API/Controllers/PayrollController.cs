using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.PayrollService;
using NB.Service.PayrollService.Dto;

namespace NB.API.Controllers
{
    [Route("api/payroll")]
    [ApiController]
    [Authorize]
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
    }
}
