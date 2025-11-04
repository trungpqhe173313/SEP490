using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.Service.AccountService;
using NB.Service.AccountService.Dto;
using NB.Service.Dto;

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
    }
}
