using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB.Service.AdminService;
using NB.Service.AdminService.Dto;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.RoleService.Dto;
using NB.Service.UserService.Dto;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IRoleService _roleService;

        public AdminController(IAdminService adminService, IRoleService roleService)
        {
            _adminService = adminService;
            _roleService = roleService;
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
    }
}
