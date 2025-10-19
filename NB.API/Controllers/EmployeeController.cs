using Microsoft.AspNetCore.Mvc;
using NB.Repository.EmployeeRepository.Dto;
using NB.Service.Common;
using NB.Service.EmployeeService;
using NB.Service.Dto;

namespace NB.API.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeeController> _logger;
        public EmployeeController(
            IEmployeeService employeeService,
            ILogger<EmployeeController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] EmployeeSearch search)
        {
            try
            {
                var result = await _employeeService.GetData(search);
                return Ok(ApiResponse<PagedList<EmployeeDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu danh mục");
                return BadRequest(ApiResponse<PagedList<EmployeeDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _employeeService.GetDto(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<EmployeeDto>.Fail("Không tìm thấy danh mục", 404));
                }
                return Ok(ApiResponse<EmployeeDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh mục với Id: {Id}", id);
                return BadRequest(ApiResponse<EmployeeDto>.Fail("Có lỗi xảy ra"));
            }
        }
    }
}
