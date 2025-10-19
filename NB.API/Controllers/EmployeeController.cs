using Microsoft.AspNetCore.Mvc;
using NB.Repository.EmployeeRepository.Dto;
using NB.Service.Common;
using NB.Service.EmployeeService;
using NB.Service.Dto;
using NB.Service.EmployeeService.ViewModels;
using NB.Model.Entities;
using AutoMapper;

namespace NB.API.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IMapper _mapper;
        public EmployeeController(
            IEmployeeService employeeService,
            IMapper mapper,
            ILogger<EmployeeController> logger)
        {
            _employeeService = employeeService;
            _mapper = mapper;
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

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] EmployeeCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                // Kiểm tra người dùng đã có nhân viên liên kết chưa
                var existing = await _employeeService.GetByUserId(model.UserId);
                if (existing != null)
                {
                    return BadRequest(ApiResponse<Employee>.Fail("Người dùng đã có nhân viên liên kết"));
                }
                // Kiểm tra số điện thoại đã tồn tại chưa
                if (!string.IsNullOrWhiteSpace(model.Phone))
                {
                    var existingPhone = await _employeeService.GetByPhone(model.Phone);
                    if (existingPhone != null)
                    {
                        return BadRequest(ApiResponse<Employee>.Fail("Số điện thoại đã được sử dụng"));
                    }
                }
                else
                {
                    return BadRequest(ApiResponse<object>.Fail("Số điện thoại không được để trống"));
                }

                //var entity = _mapper.Map<EmployeeCreateVM, Employee>(model);
                var entity = new Employee
                {
                    UserId = model.UserId,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    HireDate = model.HireDate.Value,
                    Status = model.Status
                };


                await _employeeService.CreateAsync(entity);
                return Ok(ApiResponse<Employee>.Ok(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo nhân viên");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tạo nhân viên"));
            }
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] EmployeeEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var entity = await _employeeService.GetByIdAsync(model.EmployeeId);
                // Kiểm tra số điện thoại đã tồn tại chưa
                if (!string.IsNullOrWhiteSpace(model.Phone))
                {
                    var existingPhone = await _employeeService.GetByPhone(model.Phone);
                    if (existingPhone != null && existingPhone.EmployeeId != model.EmployeeId)
                    {
                        return BadRequest(ApiResponse<Employee>.Fail("Số điện thoại đã được sử dụng"));
                    }
                }
                else
                {
                    return BadRequest(ApiResponse<object>.Fail("Số điện thoại không được để trống"));
                }

                // Kiểm tra người dùng đã có nhân viên liên kết chưa
                if (model.UserId.HasValue && model.UserId != entity.UserId) // Ensure UserId is not null
                {
                    var existingUser = await _employeeService.GetByUserId(model.UserId.Value); // Use Value to convert nullable int to int
                    if (existingUser != null)
                    {
                        return BadRequest(ApiResponse<Employee>.Fail("Người dùng đã có nhân viên liên kết"));
                    }
                }
                entity.UserId = model.UserId.Value;
                entity.FullName = model.FullName;
                entity.Phone = model.Phone;
                entity.HireDate = model.HireDate.Value;
                entity.Status = model.Status;
                await _employeeService.UpdateAsync(entity);
                return Ok(ApiResponse<Employee>.Ok(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật nhân viên");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật nhân viên"));
            }
        }
    }
}
