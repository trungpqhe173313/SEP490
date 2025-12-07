using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.API.Utils;
using NB.Service.AdminService;
using NB.Service.AdminService.Dto;
using NB.Service.Common;
using NB.Service.Core.EmailService;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.RoleService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using NB.Service.UserRoleService;
using NB.Model.Entities;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            IRoleService roleService,
            IUserService userService,
            IUserRoleService userRoleService,
            IEmailService emailService,
            ICloudinaryService cloudinaryService,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _roleService = roleService;
            _userService = userService;
            _userRoleService = userRoleService;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpPost("accounts")]
        public async Task<IActionResult> GetAllAccounts([FromBody] AccountSearch search)
        {
            try
            {
                var result = await _adminService.GetData(search);
                return Ok(ApiResponse<PagedList<AccountDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedList<AccountDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu" + ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản người dùng (bao gồm role, tên, email, trạng thái, ...)
        /// </summary>
        [HttpPut("accounts/{id}")]
        public async Task<IActionResult> UpdateAccount([FromRoute] int id, [FromBody] UpdateAccountDto dto)
        {
            try
            {
                var result = await _adminService.UpdateAccountAsync(id, dto);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật tài khoản: " + ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả các roles
        /// </summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRoles();
                return Ok(ApiResponse<List<RoleDto>>.Ok(roles));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<RoleDto>>.Fail("Có lỗi xảy ra khi lấy danh sách roles: " + ex.Message));
            }
        }

        /// <summary>
        /// Tạo tài khoản mới cho Customer (chỉ Admin)
        /// </summary>
        [HttpPost("create-customer-account")]
        public async Task<IActionResult> CreateCustomerAccount([FromForm] CreateCustomerAccountVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                // Lấy giờ Việt Nam (UTC+7)
                var vietnamTime = DateTime.UtcNow.AddHours(7);

                // Validate Username đã tồn tại chưa
                var existingUsername = await _userService.GetByUsername(model.Username);
                if (existingUsername != null)
                {
                    return BadRequest(ApiResponse<object>.Fail($"Username '{model.Username}' đã tồn tại", 400));
                }

                // Validate Email đã tồn tại chưa
                var existingEmail = await _userService.GetByEmail(model.Email);
                if (existingEmail != null)
                {
                    return BadRequest(ApiResponse<object>.Fail($"Email '{model.Email}' đã tồn tại", 400));
                }

                // Tạo mật khẩu ngẫu nhiên
                string generatedPassword = GenerateRandomPassword(12);

                // Upload ảnh lên Cloudinary nếu có
                string? imageUrl = null;
                if (model.Image != null)
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(model.Image, "users/images");
                    if (imageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                }

                // Tạo User entity
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = PasswordHasher.HashPassword(generatedPassword), // Hash password đã gen
                    FullName = model.FullName ?? model.Username, // Sử dụng FullName từ model, nếu null thì dùng Username
                    Phone = model.Phone,
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

                await _userRoleService.CreateAsync(userRole);

                // Gửi email thông báo cho khách hàng với mật khẩu đã gen
                bool emailSent = await _emailService.SendNewAccountEmailAsync(model.Email, model.Username, generatedPassword);

                if (!emailSent)
                {
                    _logger.LogWarning($"Không thể gửi email cho user {model.Email}. Tài khoản đã được tạo nhưng email thông báo thất bại.");
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

        /// <summary>
        /// Tạo mật khẩu ngẫu nhiên an toàn
        /// </summary>
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
