using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.EmailService;
using NB.Service.RoleService;
using NB.Service.UserRoleService;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using static System.DateTime;

namespace NB.Service.CustomerService
{
    public class CustomerService : ICustomerService
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CustomerService> _logger;
        private const string CUSTOMER_ROLE = "Customer";
        private const int CUSTOMER_ROLE_ID = 4;

        public CustomerService(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            ICloudinaryService cloudinaryService,
            IEmailService emailService,
            ILogger<CustomerService> logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PagedList<UserDto>> GetCustomersAsync(UserSearch search, bool isAdmin = false)
        {
            // Orchestration: Phối hợp nhiều repository
            var listUser = isAdmin 
                ? await _userService.GetAllUserForAdmin(search) ?? new List<UserDto>()
                : await _userService.GetAllUser(search) ?? new List<UserDto>();

            var role = await _roleService.GetByRoleName(CUSTOMER_ROLE);
            if (role == null)
            {
                throw new InvalidOperationException("Không tìm thấy vai trò Customer");
            }
            var userRole = await _userRoleService.GetByRoleId(role.RoleId) ?? new List<UserRole>();

            // Business Logic: Lọc customers và gán RoleName
            List<UserDto> listCustomers = new List<UserDto>();
            foreach (var ur in userRole)
            {
                var user = listUser.FirstOrDefault(u => u.UserId == ur.UserId);
                if (user != null)
                {
                    user.RoleName = CUSTOMER_ROLE;
                    listCustomers.Add(user);
                }
            }

            var pagedResult = new PagedList<UserDto>(
                items: listCustomers,
                pageIndex: search.PageIndex,
                pageSize: search.PageSize,
                totalCount: listCustomers.Count
            );

            return pagedResult;
        }

        public async Task<UserDto?> GetCustomerByIdAsync(int id)
        {
            var result = await _userService.GetByUserId(id);
            if (result == null)
            {
                return null;
            }

            // Validate nghiệp vụ: Kiểm tra role của người dùng
            var role = await _roleService.GetByRoleName(CUSTOMER_ROLE);
            if (role == null)
            {
                throw new InvalidOperationException("Không tìm thấy vai trò Customer");
            }

            var userRoles = await _userRoleService.GetByRoleId(role.RoleId) ?? new List<UserRole>();
            bool isInRole = userRoles.Any(ur => ur.UserId == result.UserId);
            
            if (!isInRole)
            {
                throw new InvalidOperationException("Người dùng không phải khách hàng");
            }

            result.RoleName = CUSTOMER_ROLE;
            return result;
        }

        public async Task<User> UpdateCustomerAsync(int id, UserEditVM model, IFormFile? image)
        {
            string? uploadedImageUrl = null;
            string? oldImageUrl = null;

            try
            {
                var entity = await _userService.GetByIdAsync(id);
                if (entity == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy khách hàng");
                }

                // Lưu URL ảnh cũ để xóa nếu cần
                oldImageUrl = entity.Image;

                // Validate nghiệp vụ: Username đã tồn tại chưa?
                if (!string.IsNullOrEmpty(model.Username) && entity.Username != model.Username)
                {
                    if (!string.IsNullOrWhiteSpace(model.Username))
                    {
                        var existingUsername = await _userService.GetByUsername(model.Username);
                        if (existingUsername != null)
                        {
                            throw new InvalidOperationException("Username đã tồn tại");
                        }
                    }
                }

                // Validate nghiệp vụ: Email đã tồn tại chưa?
                if (!string.IsNullOrEmpty(model.Email) && 
                    !string.IsNullOrEmpty(entity.Email) && 
                    entity.Email != model.Email)
                {
                    var existingEmail = await _userService.GetByEmail(model.Email);
                    if (existingEmail != null)
                    {
                        throw new InvalidOperationException("Email đã tồn tại");
                    }
                }

                // Upload ảnh sau khi đã pass tất cả validation
                if (image != null)
                {
                    uploadedImageUrl = await _cloudinaryService.UploadImageAsync(image);
                    if (uploadedImageUrl == null)
                    {
                        throw new InvalidOperationException("Không thể upload ảnh");
                    }
                }

                // Mapping DTO -> Entity
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

                // Quyết định: Cho lưu DB
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

                return entity;
            }
            catch (Exception)
            {
                // Nếu có lỗi sau khi upload ảnh mới, xóa ảnh đã upload
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
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var entity = await _userService.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Không tìm thấy khách hàng");
            }

            // Soft delete
            entity.IsActive = false;
            await _userService.UpdateAsync(entity);
            
            return true;
        }

        public async Task<string> CreateCustomerAccountAsync(CreateCustomerAccountVM model, IFormFile? image)
        {
            try
            {
                var vietnamTime = Now;

                // Validate nghiệp vụ: Username đã tồn tại chưa?
                var existingUsername = await _userService.GetByUsername(model.username);
                if (existingUsername != null)
                {
                    throw new InvalidOperationException($"Username '{model.username}' đã tồn tại");
                }

                // Validate nghiệp vụ: Email đã tồn tại chưa?
                var existingEmail = await _userService.GetByEmail(model.email);
                if (existingEmail != null)
                {
                    throw new InvalidOperationException($"Email '{model.email}' đã tồn tại");
                }

                // Business Logic: Tạo mật khẩu ngẫu nhiên
                string generatedPassword = GenerateRandomPassword(12);

                // Upload ảnh lên Cloudinary nếu có
                string? imageUrl = null;
                if (image != null)
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(image, "users/images");
                    if (imageUrl == null)
                    {
                        throw new InvalidOperationException("Không thể upload ảnh");
                    }
                }

                // Mapping ViewModel -> Entity
                var newUser = new User
                {
                    Username = model.username,
                    Email = model.email,
                    Password = PasswordHasher.HashPassword(generatedPassword),
                    FullName = model.fullName ?? model.username,
                    Phone = model.phone,
                    Image = imageUrl ?? string.Empty,
                    IsActive = true,
                    CreatedAt = vietnamTime
                };

                // Orchestration: Tạo User và UserRole
                await _userService.CreateAsync(newUser);

                var userRole = new UserRole
                {
                    UserId = newUser.UserId,
                    RoleId = CUSTOMER_ROLE_ID,
                    AssignedDate = vietnamTime
                };

                _logger.LogInformation($"[DEBUG] Trước khi save - UserId: {userRole.UserId}, RoleId: {userRole.RoleId}");
                await _userRoleService.CreateAsync(userRole);
                _logger.LogInformation($"[DEBUG] Sau khi save - UserId: {userRole.UserId}, RoleId: {userRole.RoleId}");

                // Gửi email thông báo
                bool emailSent = await _emailService.SendNewAccountEmailAsync(model.email, model.username, generatedPassword);

                if (!emailSent)
                {
                    _logger.LogWarning($"Không thể gửi email cho user {model.email}. Tài khoản đã được tạo nhưng email thông báo thất bại.");
                }

                return "Đã tạo tài khoản thành công";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo tài khoản customer");
                throw;
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
