using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Model.Entities;
using NB.Service.Core.Mapper;
using NB.Service.UserRoleService;
using NB.Service.RoleService;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using NB.Service.UserRoleService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/employees")]
    public class EmployeeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IMapper _mapper;
        public EmployeeController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            IMapper mapper,
            ILogger<EmployeeController> logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] UserSearch search)
        {
            try
            {
                var roleName = "Employee";
                var listUser = await _userService.GetAllUser(search) ?? new List<UserDto>();
                var role = await _roleService.GetByRoleName(roleName);
                var userRole = await _userRoleService.GetByRoleId(role.RoleId) ?? new List<UserRole>();
                List<UserDto> listEmployee = new List<UserDto>();
                foreach (var ur in userRole)
                {
                    var user = listUser.FirstOrDefault(u => u.UserId == ur.UserId);
                    if (user != null)
                    {
                        user.RoleName = roleName;
                        listEmployee.Add(user);
                    }
                }
                var pagedResult = new PagedList<UserDto>(
                    items: listEmployee,
                    pageIndex: search.PageIndex,
                    pageSize: search.PageSize,
                    totalCount: listEmployee.Count
                );

                return Ok(ApiResponse<PagedList<UserDto>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu nhân viên");
                return BadRequest(ApiResponse<PagedList<UserDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("GetByUserId/{id}")]
        public async Task<IActionResult> GetByUserId(int id)
        {
            try
            {
                var result = await _userService.GetByUserId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy nhân viên", 404));
                }
                result.RoleName = "Employee";
                return Ok(ApiResponse<UserDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy nhân viên với Id: {Id}", id);
                return BadRequest(ApiResponse<UserDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] UserCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                var roleName = "Employee";
                // Kiểm tra email da ton tai chua
                var existingEmail = await _userService.GetByEmail(model.Email);
                if (existingEmail != null)
                {
                    return BadRequest(ApiResponse<User>.Fail("Email đã tồn tại"));
                }
                // Kiểm tra username đã tồn tại chưa
                if (!string.IsNullOrWhiteSpace(model.Username))
                {
                    var existingUsername = await _userService.GetByUsername(model.Username);
                    if (existingUsername != null)
                    {
                        return BadRequest(ApiResponse<User>.Fail("Username đã tồn tại"));
                    }
                }

                var entity = _mapper.Map<UserCreateVM, User>(model);
                entity.IsActive = true;
                entity.CreatedAt = DateTime.Now;
                //var entity = new Employee
                //{
                //    UserId = model.UserId,
                //    FullName = model.FullName,
                //    Phone = model.Phone,
                //    HireDate = model.HireDate.Value,
                //    Status = model.Status
                //};
                await _userService.CreateAsync(entity);

                var role = await _roleService.GetByRoleName(roleName);
                var entityUserRole = _mapper.Map<UserRoleCreateVM, UserRole>(
                    new UserRoleCreateVM
                    {
                        RoleId = role.RoleId,
                        UserId = entity.UserId,
                    }
                    );
                entityUserRole.AssignedDate = DateTime.Now;
                await _userRoleService.CreateAsync(entityUserRole);

                return Ok(ApiResponse<User>.Ok(new User
                {
                    UserId = entity.UserId,
                    FullName = entity.FullName,
                    Email = entity.Email,
                    Image = entity.Image,
                    Username = entity.Username,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo nhân viên");
                return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi tạo nhân viên"));
            }
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] UserEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<User>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var entity = await _userService.GetByIdAsync(model.UserId);


                // Kiểm tra email da ton tai chua
                var existingEmail = await _userService.GetByEmail(model.Email);
                if (existingEmail != null)
                {
                    return BadRequest(ApiResponse<User>.Fail("Email đã tồn tại"));
                }
                // Kiểm tra username đã tồn tại chưa
                if (!string.IsNullOrWhiteSpace(model.Username))
                {
                    var existingUsername = await _userService.GetByUsername(model.Username);
                    if (existingUsername != null)
                    {
                        return BadRequest(ApiResponse<User>.Fail("Username đã tồn tại"));
                    }
                }

                _mapper.Map(model, entity);
                await _userService.UpdateAsync(entity);
                return Ok(ApiResponse<User>.Ok(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật nhân viên");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật nhân viên"));
            }
        }
    }
}
