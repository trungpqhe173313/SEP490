using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.API.Utils;
using NB.Service.AccountService;
using NB.Service.AccountService.Dto;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using System.Security.Claims;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ICloudinaryService _cloudinaryService;

        public AccountController(IAccountService accountService, ICloudinaryService cloudinaryService)
        {
            _accountService = accountService;
            _cloudinaryService = cloudinaryService;
        }
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var result = await _accountService.LoginAsync(request.Username, request.Password);
            return StatusCode(result.StatusCode, result);
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Không thể xác định người dùng");

            var userId = int.Parse(userIdClaim);
            var result = await _accountService.LogoutAsync(userId);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<RefreshTokenResponse>.Fail("Invalid request data", 400));
                }
                var result = await _accountService.RefreshTokenAsync(request.RefreshToken);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<RefreshTokenResponse>.Fail("Internal server error", 500));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<bool>.Fail("Dữ liệu không hợp lệ", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(ApiResponse<bool>.Fail("Không thể xác định người dùng", 401));
                }

                var result = await _accountService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.Fail("Lỗi hệ thống", 500));
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ForgotPasswordResponse>.Fail("Dữ liệu không hợp lệ", 400));
                }

                var result = await _accountService.ForgotPasswordAsync(request.Email);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ForgotPasswordResponse>.Fail("Lỗi hệ thống", 500));
            }
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<VerifyOtpResponse>.Fail("Dữ liệu không hợp lệ", 400));
                }

                var result = await _accountService.VerifyOtpAsync(request.Email, request.OtpCode);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<VerifyOtpResponse>.Fail("Lỗi hệ thống", 500));
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<bool>.Fail("Dữ liệu không hợp lệ", 400));
                }

                var result = await _accountService.ResetPasswordAsync(request.ResetToken, request.NewPassword);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.Fail("Lỗi hệ thống", 500));
            }
        }
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(ApiResponse<UserDto>.Fail("Không thể xác định người dùng", 401));

            var userId = int.Parse(userIdClaim);
            var result = await _accountService.GetProfileAsync(userId);

            return StatusCode(result.StatusCode, result);
        }


        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.Fail("Dữ liệu không hợp lệ", 400));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(ApiResponse<bool>.Fail("Không thể xác định người dùng", 401));

            // Validate image file type 
            if (request.imageFile != null)
            {
                var imageExtension = Path.GetExtension(request.imageFile.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<bool>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }

                // Upload image to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(request.imageFile, "users/images");
                if (imageUrl == null)
                {
                    return BadRequest(ApiResponse<bool>.Fail("Không thể upload ảnh", 400));
                }

                // Set image và uploaded URL 
                request.image = imageUrl;
            }

            var userId = int.Parse(userIdClaim);
            var result = await _accountService.UpdateProfileAsync(userId, request);

            return StatusCode(result.StatusCode, result);
        }
    }
}
