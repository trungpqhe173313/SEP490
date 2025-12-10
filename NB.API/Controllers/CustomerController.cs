using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.UserRoleService.ViewModels;
using NB.Service.UserRoleService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using NB.Service.UserService;
using NB.Service.Core.Mapper;
using NB.API.Utils;
using NB.Service.Core.EmailService;
using static System.DateTime;

namespace NB.API.Controllers
{
    [Route("api/customers")]
    public class CustomerController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private readonly ILogger<CustomerController> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly string roleName = "Customer";
        public CustomerController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            IMapper mapper,
            ILogger<CustomerController> logger,
            ICloudinaryService cloudinaryService,
            IEmailService emailService)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _mapper = mapper;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
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
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu khách hàng");
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
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy khách hàng", 404));
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
                _logger.LogError(ex, "Lỗi khi lấy khách hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<UserDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpPut("UpdateCustomer/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromForm] UserEditVM model)
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
                    return NotFound(ApiResponse<User>.Fail("Không tìm thấy khách hàng"));
                }

                // Lưu URL ảnh cũ để xóa nếu cần
                oldImageUrl = entity.Image;

                // Validate tất cả TRƯỚC khi upload ảnh
                // Kiểm tra nếu username thay đỏi thì username đã tồn tại chưa
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
                // Kiểm tra nếu email thay đổi thì email da ton tai chua
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

                _logger.LogError(ex, "Lỗi khi cập nhật khách hàng");
                return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi cập nhật khách hàng"));
            }
        }

        [HttpDelete("DeleteCustomer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var entity = await _userService.GetByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<User>.Fail("Không tìm thấy khách hàng"));
                }
                entity.IsActive = false;
                await _userService.UpdateAsync(entity);
                return Ok(ApiResponse<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa khách hàng với ID: {id}", id);
                return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi xóa khách hàng"));
            }
        }

        /// <summary>
        /// Tạo tài khoản mới cho Customer (Manager)
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpPost("CreateCustomerAccount")]
        public async Task<IActionResult> CreateCustomerAccount([FromForm] CreateCustomerAccountVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                // Lấy giờ Việt Nam (UTC+7)
                var vietnamTime = Now;

                // Validate Username đã tồn tại chưa
                var existingUsername = await _userService.GetByUsername(model.username);
                if (existingUsername != null)
                {
                    return BadRequest(ApiResponse<object>.Fail($"Username '{model.username}' đã tồn tại", 400));
                }

                // Validate Email đã tồn tại chưa
                var existingEmail = await _userService.GetByEmail(model.email);
                if (existingEmail != null)
                {
                    return BadRequest(ApiResponse<object>.Fail($"Email '{model.email}' đã tồn tại", 400));
                }

                // Tạo mật khẩu ngẫu nhiên
                string generatedPassword = GenerateRandomPassword(12);

                // Upload ảnh lên Cloudinary nếu có
                string? imageUrl = null;
                if (model.image != null)
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(model.image, "users/images");
                    if (imageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                }

                // Tạo User entity
                var newUser = new User
                {
                    Username = model.username,
                    Email = model.email,
                    Password = PasswordHasher.HashPassword(generatedPassword), // Hash password đã gen
                    FullName = model.fullName ?? model.username, // Sử dụng FullName từ model, nếu null thì dùng Username
                    Phone = model.phone,
                    Image = imageUrl ?? string.Empty, // Lưu relative path từ Cloudinary
                    IsActive = true,
                    CreatedAt = vietnamTime
                };

                // Tạo User trong database
                await _userService.CreateAsync(newUser);

                // Sau khi tạo User thành công, tạo UserRole
                var userRole = new UserRole
                {
                    UserId = newUser.UserId,
                    RoleId = 4, // Customer role
                    AssignedDate = vietnamTime
                };

                _logger.LogInformation($"[DEBUG] Trước khi save - UserId: {userRole.UserId}, RoleId: {userRole.RoleId}");
                await _userRoleService.CreateAsync(userRole);
                _logger.LogInformation($"[DEBUG] Sau khi save - UserId: {userRole.UserId}, RoleId: {userRole.RoleId}");

                // Gửi email thông báo cho khách hàng với mật khẩu đã gen
                bool emailSent = await _emailService.SendNewAccountEmailAsync(model.email, model.username, generatedPassword);

                if (!emailSent)
                {
                    _logger.LogWarning($"Không thể gửi email cho user {model.email}. Tài khoản đã được tạo nhưng email thông báo thất bại.");
                }

                // Trả về thông tin user đã tạo (không bao gồm password)
                var userDto = await _userService.GetByUserId(newUser.UserId);

                return Ok(ApiResponse<Object>.Ok("Đã tạo tài khoản thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo tài khoản customer");
                return BadRequest(ApiResponse<object>.Fail($"Có lỗi xảy ra khi tạo tài khoản: {ex.Message}", 400));
            }
        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*";
            const string allChars = upperCase + lowerCase + digits + specialChars;

            var random = new Random();
            var password = new char[length];

            // Đảm bảo mật khẩu có ít nhất 1 ký tự từ mỗi loại
            password[0] = upperCase[random.Next(upperCase.Length)];
            password[1] = lowerCase[random.Next(lowerCase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = specialChars[random.Next(specialChars.Length)];

            // Fill phần còn lại với ký tự ngẫu nhiên
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle để tránh pattern cố định
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}
