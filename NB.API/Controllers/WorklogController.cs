using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.WorklogService;
using NB.Service.WorklogService.Dto;
using NB.Service.WorklogService.ViewModels;
using System.Security.Claims;

namespace NB.API.Controllers
{
    [Route("api/worklogs")]
    [ApiController]
    [Authorize]

    public class WorklogController : ControllerBase
    {
        private readonly IWorklogService _worklogService;
        private readonly ILogger<WorklogController> _logger;

        public WorklogController(
            IWorklogService worklogService,
            ILogger<WorklogController> logger)
        {
            _worklogService = worklogService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo worklog (chấm công) cho nhân viên - Hỗ trợ nhiều công việc cùng lúc
        /// - Admin tạo worklog cho nhân viên bằng cách chọn EmployeeId
        /// - Có thể chọn nhiều công việc (Jobs) cùng lúc trong 1 request
        /// - Hệ thống kiểm tra EmployeeId có role Employee (RoleId = 3) không
        /// - Nếu Job.PayType = Per_Ngay → hệ thống tự set Quantity = 1
        /// - Nếu Job.PayType = Per_Tan → phải nhập Quantity (số tấn)
        /// - Trả về danh sách thành công/thất bại cho từng công việc
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateWorklog([FromBody] CreateWorklogBatchDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<CreateWorklogBatchResponseVM>.Fail(string.Join(", ", errors)));
                }

                var result = await _worklogService.CreateWorklogBatchAsync(dto);
                return Ok(ApiResponse<CreateWorklogBatchResponseVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo worklog");
                return BadRequest(ApiResponse<CreateWorklogBatchResponseVM>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách worklog của nhân viên trong ngày
        /// - Admin có thể xem worklog của bất kỳ nhân viên nào
        /// - Trả về tất cả các công việc mà nhân viên đã làm trong ngày đó
        /// </summary>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetWorklogsByEmployeeAndDate([FromBody] GetWorklogsByEmployeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<List<WorklogResponseVM>>.Fail(string.Join(", ", errors)));
                }

                var worklogs = await _worklogService.GetWorklogsByEmployeeAndDateAsync(dto.EmployeeId, dto.WorkDate);
                return Ok(ApiResponse<List<WorklogResponseVM>>.Ok(worklogs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy worklog của nhân viên {EmployeeId} ngày {WorkDate}", dto.EmployeeId, dto.WorkDate);
                return BadRequest(ApiResponse<List<WorklogResponseVM>>.Fail(ex.Message));
            }
        }
    }
}

