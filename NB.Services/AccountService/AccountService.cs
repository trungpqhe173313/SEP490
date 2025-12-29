using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Repository.RoleRepository;
using NB.Repository.UserRolerRepository;
using NB.Service.AccountService.Dto;
using NB.Service.Common;
using NB.Service.Core.EmailService;
using NB.Service.Core.JwtService;
using NB.Service.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService
{
    public class AccountService : Service<User>,IAccountService
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IUserRolerRepository _userRoleRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IUserService userService,
            IRepository<User> userRepository,
            IJwtService jwtService,
            IUserRolerRepository userRolerRepository,
            IRoleRepository roleRepository,
            IEmailService emailService,
            ICloudinaryService cloudinaryService,
            ILogger<AccountService> logger) : base(userRepository)
        {
            _userService = userService;
            _jwtService = jwtService;
            _userRoleRepository = userRolerRepository;
            _roleRepository = roleRepository;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(password))
                return ApiResponse<LoginResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác", 400);

            var user = await _userService.GetByUsernameForLogin(username);
            if (user == null)
                return ApiResponse<LoginResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác", 400);
            if (user.IsActive != true) 
               return ApiResponse<LoginResponse>.Fail("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.", 403); 
            var result = await _userService.CheckPasswordAsync(user, password);
            if (!result)
            {
                return ApiResponse<LoginResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác", 400);
            }
            return await GenToken(user);
        }

        public async Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (!_jwtService.ValidateRefreshToken(refreshToken))
            {
                return ApiResponse<RefreshTokenResponse>.Fail("Invalid refresh token format", 401);
            }
            var user = await _userService.GetByRefreshTokenAsync(refreshToken);
            if (user == null)
                return ApiResponse<RefreshTokenResponse>.Fail("Invalid refresh token", 401);
            if(user.RefreshTokenExpiryDate < DateTime.Now)
                return ApiResponse<RefreshTokenResponse>.Fail("Refresh token has expired", 401);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            var refreshTokenExpiryDate = _jwtService.GetRefreshTokenExpiry();

            var accessTokenExpiry = _jwtService.GetAccessTokenExpiry();

            user.RefreshToken = newRefreshToken;

            user.RefreshTokenExpiryDate = refreshTokenExpiryDate;

            await _userService.UpdateAsync(user);

            var tokenResponse = await GenToken(user);
            var userDto = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
                // Lấy roles tương tự phương thức GenToken
                Roles = _userRoleRepository.GetQueryable()
              .Where(x => x.UserId == user.UserId)
              .Join(_roleRepository.GetQueryable(),
                  userRole => userRole.RoleId,
                  role => role.RoleId,
                  (userRole, role) => role.RoleName)
              .Distinct()
              .ToList()
            };
            var newAccessToken = _jwtService.GenerateToken(userDto);

            return ApiResponse<RefreshTokenResponse>.Ok(new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = accessTokenExpiry
            });
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(oldPassword))
                return ApiResponse<bool>.Fail("Mật khẩu cũ không được để trống", 400);

            if (string.IsNullOrEmpty(newPassword))
                return ApiResponse<bool>.Fail("Mật khẩu mới không được để trống", 400);

            var user = await GetQueryable().FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null)
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng", 404);
            var isOldPasswordCorrect = await _userService.CheckPasswordAsync(user, oldPassword);
            if (!isOldPasswordCorrect)
                return ApiResponse<bool>.Fail("Mật khẩu cũ không chính xác", 400);

            if (oldPassword == newPassword)
                return ApiResponse<bool>.Fail("Mật khẩu mới phải khác mật khẩu cũ", 400);

            // Hash password mới trước khi lưu
            user.Password = PasswordHasher.HashPassword(newPassword);
            await _userService.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return ApiResponse<ForgotPasswordResponse>.Fail("Email không được để trống", 400);

            var user = await _userService.GetByEmail(email);
            if (user == null)
            {
                // Trả về success để không tiết lộ email có tồn tại hay không (security best practice)
                return ApiResponse<ForgotPasswordResponse>.Ok(new ForgotPasswordResponse
                {
                    OtpCode = string.Empty,
                    ExpiresAt = DateTime.Now,
                    Message = "Nếu email tồn tại, chúng tôi đã gửi mã OTP đến email của bạn"
                });
            }

            // Tạo mã OTP 6 số
            var otpCode = GenerateOtpCode();
            var otpExpiry = DateTime.Now.AddMinutes(10); // OTP có hiệu lực 10 phút

            // Lưu OTP vào RefreshToken field với prefix để phân biệt
            var otpWithPrefix = $"OTP_{otpCode}";
            
            var userEntity = await _userService.GetByIdAsync(user.UserId);
            if (userEntity != null)
            {
                userEntity.RefreshToken = otpWithPrefix;
                userEntity.RefreshTokenExpiryDate = otpExpiry;
                await _userService.UpdateAsync(userEntity);
            }

            // Gửi email với mã OTP
            try
            {
                var emailSent = await _emailService.SendOtpEmailAsync(user.Email, otpCode, user.FullName);
                if (!emailSent)
                {
                    _logger.LogWarning("Không thể gửi email OTP tới {Email} vào {Time}", user.Email, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email OTP cho {Email} vào {Time}", user.Email, DateTime.Now);
            }
            return ApiResponse<ForgotPasswordResponse>.Ok(new ForgotPasswordResponse
            {
                OtpCode = string.Empty, // Không trả về OTP trong response (bảo mật)
                ExpiresAt = otpExpiry,
                Message = "Đã gửi mã OTP đến email của bạn"
            });
        }

        public async Task<ApiResponse<VerifyOtpResponse>> VerifyOtpAsync(string email, string otpCode)
        {
            if (string.IsNullOrEmpty(email))
                return ApiResponse<VerifyOtpResponse>.Fail("Email không được để trống", 400);

            if (string.IsNullOrEmpty(otpCode))
                return ApiResponse<VerifyOtpResponse>.Fail("Mã OTP không được để trống", 400);

            // Tìm user theo email
            var user = await _userService.GetByEmail(email);
            if (user == null)
                return ApiResponse<VerifyOtpResponse>.Fail("Email không tồn tại", 404);

            // Kiểm tra OTP
            var otpWithPrefix = $"OTP_{otpCode}";
            var userEntity = await _userService.GetByIdAsync(user.UserId);
            
            if (userEntity == null)
                return ApiResponse<VerifyOtpResponse>.Fail("Không tìm thấy người dùng", 404);

            // Kiểm tra OTP có khớp không
            if (userEntity.RefreshToken != otpWithPrefix)
                return ApiResponse<VerifyOtpResponse>.Fail("Mã OTP không chính xác", 400);

            // Kiểm tra OTP còn hiệu lực không
            if (userEntity.RefreshTokenExpiryDate == null || userEntity.RefreshTokenExpiryDate < DateTime.Now)
                return ApiResponse<VerifyOtpResponse>.Fail("Mã OTP đã hết hạn", 400);

            // OTP đúng, tạo reset token để cho phép reset password
            var resetToken = _jwtService.GenerateRefreshToken();
            var resetTokenExpiry = DateTime.Now.AddMinutes(15); // Reset token có hiệu lực 15 phút

            // Lưu reset token vào RefreshToken field (thay thế OTP)
            var resetTokenWithPrefix = $"RESET_{resetToken}";
            userEntity.RefreshToken = resetTokenWithPrefix;
            userEntity.RefreshTokenExpiryDate = resetTokenExpiry;
            await _userService.UpdateAsync(userEntity);

            return ApiResponse<VerifyOtpResponse>.Ok(new VerifyOtpResponse
            {
                ResetToken = resetToken,
                ExpiresAt = resetTokenExpiry,
                Message = "Xác thực OTP thành công. Bạn có thể đặt lại mật khẩu."
            });
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string resetToken, string newPassword)
        {
            if (string.IsNullOrEmpty(resetToken))
                return ApiResponse<bool>.Fail("Reset token không được để trống", 400);

            if (string.IsNullOrEmpty(newPassword))
                return ApiResponse<bool>.Fail("Mật khẩu mới không được để trống", 400);

            // Tìm user có reset token matching
            var resetTokenWithPrefix = $"RESET_{resetToken}";
            var user = await _userService.GetByRefreshTokenAsync(resetTokenWithPrefix);
            
            if (user == null)
                return ApiResponse<bool>.Fail("Reset token không hợp lệ hoặc đã hết hạn", 400);

            // Kiểm tra token còn hiệu lực không
            if (user.RefreshTokenExpiryDate == null || user.RefreshTokenExpiryDate < DateTime.Now)
                return ApiResponse<bool>.Fail("Reset token đã hết hạn", 400);

            // Đặt lại mật khẩu
            var userEntity = await _userService.GetByIdAsync(user.UserId);
            if (userEntity != null)
            {
                // Hash password trước khi lưu
                userEntity.Password = PasswordHasher.HashPassword(newPassword);
                // Xóa reset token sau khi đã sử dụng
                userEntity.RefreshToken = null;
                userEntity.RefreshTokenExpiryDate = null;
                await _userService.UpdateAsync(userEntity);
            }

            return ApiResponse<bool>.Ok(true);
        }

        /// <summary>
        /// Tạo mã OTP 6 số ngẫu nhiên
        /// </summary>
        private string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Tạo số từ 100000 đến 999999
        }

        private async Task<ApiResponse<LoginResponse>> GenToken(UserDto? user)
        {
            if (user == null)
                return ApiResponse<LoginResponse>.Fail("User not found", 401);

            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiryDate = _jwtService.GetRefreshTokenExpiry();
            var accessTokenExpiry = _jwtService.GetAccessTokenExpiry();
            var userDto = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Image = user.Image,
            };

            var userRefreshToken = await _userService.GetByIdAsync(user.UserId);
            if(userRefreshToken != null)
            {
                userRefreshToken.RefreshToken = refreshToken;
                userRefreshToken.RefreshTokenExpiryDate = refreshTokenExpiryDate;
                await _userService.UpdateAsync(userRefreshToken);
            }

            var roleFromUser = _userRoleRepository.GetQueryable()
               .Where(x => x.UserId == user.UserId)
               .Join(_roleRepository.GetQueryable(),
                   userRole => userRole.RoleId,
                   role => role.RoleId,
                   (userRole, role) => role.RoleName)
               .Distinct()
               .ToList();

            var sumRole = roleFromUser.ToList();
            userDto.Roles = sumRole;

            var token = _jwtService.GenerateToken(userDto);

            return ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresAt = accessTokenExpiry, 
                UserInfo = userDto
            });
        }

        public async Task<ApiResponse<bool>> LogoutAsync(int userId)
        {
            var user = await GetQueryable().FirstOrDefaultAsync(u => u.UserId == userId); ;
            if (user == null)
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng", 404);

            user.RefreshToken = null;
            user.RefreshTokenExpiryDate = null;
            await _userService.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true);
        }
        public async Task<ApiResponse<UserInfo>> GetProfileAsync(int userId)
        {
            var user = await GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return ApiResponse<UserInfo>.Fail("Không tìm thấy người dùng", 404);

            var roles = _userRoleRepository.GetQueryable()
                .Where(x => x.UserId == userId)
                .Join(_roleRepository.GetQueryable(),
                    ur => ur.RoleId,
                    r => r.RoleId,
                    (ur, r) => r.RoleName)
                .Distinct()
                .ToList();

            var userDto = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Image = user.Image,
                Roles = roles
            };

            return ApiResponse<UserInfo>.Ok(userDto);
        }

        public async Task<ApiResponse<bool>> UpdateProfileAsync(int userId, UpdateProfileDto request)
        {
            if (request == null)
                return ApiResponse<bool>.Fail("Dữ liệu không hợp lệ", 400);

            var user = await GetQueryable().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng", 404);

            // Upload image lên Cloudinary nếu có 
            if (request.imageFile != null)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(request.imageFile, "users/images");
                if (imageUrl == null)
                {
                    return ApiResponse<bool>.Fail("Không thể upload ảnh", 400);
                }
                request.image = imageUrl;
            }

            if (!string.IsNullOrWhiteSpace(request.phone))
            {
                bool phoneExists = await GetQueryable()
                    .AnyAsync(u => u.Phone == request.phone && u.UserId != userId);

                if (phoneExists)
                    return ApiResponse<bool>.Fail("Số điện thoại đã tồn tại", 409);
            }
            if (!string.IsNullOrWhiteSpace(request.email))
            {
                bool emailExists = await GetQueryable()
                    .AnyAsync(u => u.Email == request.email && u.UserId != userId);

                if (emailExists)
                    return ApiResponse<bool>.Fail("Email đã tồn tại", 409);
            }
            user.FullName = request.fullName ?? user.FullName;
            user.Email = request.email ?? user.Email;
            user.Phone = request.phone ?? user.Phone;
            user.Image = request.image ?? user.Image;

            await _userService.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true);
        }

    }
}
