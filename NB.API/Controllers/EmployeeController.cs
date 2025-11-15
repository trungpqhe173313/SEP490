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
using NB.API.Utils;

namespace NB.API.Controllers
{
    [Route("api/employees")]
    public class EmployeeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly string roleName = "Employee";
        public EmployeeController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            IMapper mapper,
            ILogger<EmployeeController> logger,
            ICloudinaryService cloudinaryService)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _mapper = mapper;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] UserSearch search)
        {
            try
            {
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

        [HttpPost("GetDataForAdmin")]
        public async Task<IActionResult> GetDataForAdmin([FromBody] UserSearch search)
        {
            try
            {
                var listUser = await _userService.GetAllUserForAdmin(search) ?? new List<UserDto>();
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
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu khách hàng");
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
                //kiem tra role của người dùng
                var role = await _roleService.GetByRoleName(roleName);
                if (role == null)
                {
                    return BadRequest(ApiResponse<UserDto>.Fail("Không tìm thấy vai trò", 404));
                }

                var userRoles = await _userRoleService.GetByRoleId(role.RoleId) ?? new List<UserRole>();

                bool isInRole = userRoles.Any(ur => ur.UserId == result.UserId);
                if (!isInRole)
                {
                    return BadRequest(ApiResponse<UserDto>.Fail("Người dùng không phải nhân viên", 404));
                }

                result.RoleName = roleName;
                return Ok(ApiResponse<UserDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy nhân viên với Id: {Id}", id);
                return BadRequest(ApiResponse<UserDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpPost("CreateEmployee")]
        public async Task<IActionResult> CreateEmployee([FromForm] UserCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<User>.Fail("Dữ liệu không hợp lệ"));
            }

            //nếu ảnh không null thì kiểm tra định dạng
            if (model.Image != null)
            {
                var imageExtension = Path.GetExtension(model.Image.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }
            }

            string? uploadedImageUrl = null;
            try
            {
                // Validate email và username TRƯỚC khi upload ảnh
                // Kiểm tra email da ton tai chua
                if (!string.IsNullOrEmpty(model.Email))
                {
                    var existingEmail = await _userService.GetByEmail(model.Email);
                    if (existingEmail != null)
                    {
                        return BadRequest(ApiResponse<User>.Fail("Email đã tồn tại"));
                    }
                }
                
                // Kiểm tra username đã tồn tại chưa
                var existingUsername = await _userService.GetByUsername(model.Username);
                if (existingUsername != null)
                {
                    return BadRequest(ApiResponse<User>.Fail("Username đã tồn tại"));
                }

                // Chỉ upload ảnh sau khi đã pass tất cả validation
                if (model.Image != null)
                {
                    uploadedImageUrl = await _cloudinaryService.UploadImageAsync(model.Image);
                    if (uploadedImageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                }

                var entity = _mapper.Map<UserCreateVM, User>(model);
                entity.Password = "123"; // Mật khẩu mặc định
                entity.IsActive = true;
                entity.CreatedAt = DateTime.Now;
                entity.Image = uploadedImageUrl;
                await _userService.CreateAsync(entity);

                // Gán role cho nhân viên
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
                    Password = entity.Password,
                    Phone = entity.Phone,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                // Nếu có lỗi sau khi upload ảnh, xóa ảnh đã upload để tránh lãng phí storage
                if (uploadedImageUrl != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _cloudinaryService.DeleteFileAsync(uploadedImageUrl);
                            _logger.LogInformation($"Đã xóa ảnh không sử dụng: {uploadedImageUrl}");
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, $"Không thể xóa ảnh đã upload: {uploadedImageUrl}");
                        }
                    });
                }

                _logger.LogError(ex, "Lỗi khi tạo nhân viên");
                return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi tạo nhân viên"));
            }
        }

        [HttpPut("UpdateEmployee/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromForm] UserEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<User>.Fail("Dữ liệu không hợp lệ"));
            }

            //nếu ảnh không null thì kiểm tra định dạng
            if (model.Image != null)
            {
                var imageExtension = Path.GetExtension(model.Image.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }
            }

            string? uploadedImageUrl = null;
            string? oldImageUrl = null;
            try
            {
                var entity = await _userService.GetByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<User>.Fail("Không tìm thấy nhân viên"));
                }

                // Lưu URL ảnh cũ để xóa nếu cần
                oldImageUrl = entity.Image;

                // Validate tất cả TRƯỚC khi upload ảnh
                // Kiểm tra nếu username thay đổi thì username đã tồn tại chưa
                if (!string.IsNullOrEmpty(model.Username)
                    && entity.Username != model.Username)
                {
                    if (!string.IsNullOrWhiteSpace(model.Username))
                    {
                        var existingUsername = await _userService.GetByUsername(model.Username);
                        if (existingUsername != null)
                        {
                            return BadRequest(ApiResponse<User>.Fail("Username đã tồn tại"));
                        }
                    }
                }
                // Kiểm tra nếu email thay đổi thì email đã tồn tại chưa 
                if (!string.IsNullOrEmpty(model.Email)
                    && !string.IsNullOrEmpty(entity.Email)
                    && entity.Email != model.Email)
                {
                    var existingEmail = await _userService.GetByEmail(model.Email);
                    if (existingEmail != null)
                    {
                        return BadRequest(ApiResponse<User>.Fail("Email đã tồn tại"));
                    }
                }

                // Chỉ upload ảnh sau khi đã pass tất cả validation
                if (model.Image != null)
                {
                    uploadedImageUrl = await _cloudinaryService.UploadImageAsync(model.Image);
                    if (uploadedImageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                }

                // Map các thông tin khác từ model vào entity
                if (!string.IsNullOrEmpty(model.Username))
                {
                    entity.Username = model.Username;
                }
                if (!string.IsNullOrEmpty(model.Email))
                {
                    entity.Email = model.Email;
                }
                if (!string.IsNullOrEmpty(model.Password))
                {
                    entity.Password = model.Password;
                }
                if (!string.IsNullOrEmpty(model.FullName))
                {
                    entity.FullName = model.FullName;
                }
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    entity.Phone = model.Phone;
                }
                if (model.IsActive.HasValue)
                {
                    entity.IsActive = model.IsActive.Value;
                }

                // Cập nhật ảnh nếu có ảnh mới
                if (uploadedImageUrl != null)
                {
                    entity.Image = uploadedImageUrl;
                }

                await _userService.UpdateAsync(entity);

                // Xóa ảnh cũ nếu có ảnh mới
                if (uploadedImageUrl != null && !string.IsNullOrEmpty(oldImageUrl))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _cloudinaryService.DeleteFileAsync(oldImageUrl);
                            _logger.LogInformation($"Đã xóa ảnh cũ: {oldImageUrl}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Không thể xóa ảnh cũ: {oldImageUrl}");
                        }
                    });
                }

                return Ok(ApiResponse<User>.Ok(entity));
            }
            catch (Exception ex)
            {
                // Nếu có lỗi sau khi upload ảnh mới, xóa ảnh đã upload để tránh lãng phí storage
                if (uploadedImageUrl != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _cloudinaryService.DeleteFileAsync(uploadedImageUrl);
                            _logger.LogInformation($"Đã xóa ảnh không sử dụng: {uploadedImageUrl}");
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, $"Không thể xóa ảnh đã upload: {uploadedImageUrl}");
                        }
                    });
                }

                _logger.LogError(ex, "Lỗi khi cập nhật nhân viên");
                return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi cập nhật nhân viên"));
            }
        }

        [HttpDelete("DeleteEmployee/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var entity = await _userService.GetByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<User>.Fail("Không tìm thấy nhân viên"));
                }
                entity.IsActive = false;
                await _userService.UpdateAsync(entity);
                return Ok(ApiResponse<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa nhân viên với ID: {id}", id);
                return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi xóa nhân viên"));
            }
        }
    }
}
