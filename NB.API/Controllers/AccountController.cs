using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.Service.AccountService;
using NB.Service.AccountService.Dto;
using NB.Service.Dto;
using System.Security.Claims;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var result = await _accountService.LoginAsync(request.Username, request.Password);
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
    }
}
