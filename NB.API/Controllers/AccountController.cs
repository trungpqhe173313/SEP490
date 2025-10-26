using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.Service.AccountService;
using NB.Service.AccountService.Dto;

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
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var result = await _accountService.LoginAsync(request.Username, request.Password);
            return StatusCode(result.StatusCode, result);
        }
    }
}
