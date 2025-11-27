using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.JobService;
using NB.Service.JobService.Dto;

namespace NB.API.Controllers
{
    [Route("api/jobs")]
    [ApiController]
    [Authorize]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ILogger<JobController> _logger;

        public JobController(
            IJobService jobService,
            ILogger<JobController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả công việc
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllJobs()
        {
            try
            {
                var jobs = await _jobService.GetAllJobsAsync();
                return Ok(ApiResponse<List<JobDto>>.Ok(jobs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách công việc");
                return BadRequest(ApiResponse<List<JobDto>>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Lấy chi tiết công việc theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById([FromRoute] int id)
        {
            try
            {
                var job = await _jobService.GetJobByIdAsync(id);
                return Ok(ApiResponse<JobDto>.Ok(job));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy công việc {Id}", id);
                return BadRequest(ApiResponse<JobDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Tạo công việc mới
        /// - PayType: 'Per_Ngay' hoặc 'Per_Tan'
        /// - Rate: Đơn giá (VNĐ/ngày hoặc VNĐ/tấn)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<JobDto>.Fail(string.Join(", ", errors)));
                }

                var job = await _jobService.CreateJobAsync(dto);
                return Ok(ApiResponse<JobDto>.Ok(job));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo công việc");
                return BadRequest(ApiResponse<JobDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật công việc
        /// - Có thể thay đổi tên, loại tính công, đơn giá, trạng thái
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateJob([FromBody] UpdateJobDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<JobDto>.Fail(string.Join(", ", errors)));
                }

                var job = await _jobService.UpdateJobAsync(dto);
                return Ok(ApiResponse<JobDto>.Ok(job));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật công việc {Id}", dto.Id);
                return BadRequest(ApiResponse<JobDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Xóa công việc (Soft Delete)
        /// - Chuyển IsActive = false thay vì xóa vật lý
        /// - Bảo toàn dữ liệu và các worklog liên quan
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob([FromRoute] int id)
        {
            try
            {
                var result = await _jobService.DeleteJobAsync(id);
                return Ok(ApiResponse<bool>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa công việc {Id}", id);
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }
    }
}
