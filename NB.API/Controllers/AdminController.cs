using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.Service.AdminService;
using NB.Service.AdminService.Dto;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.UserService.Dto;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
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
    }
}
