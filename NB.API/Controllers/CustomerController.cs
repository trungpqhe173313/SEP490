using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.CustomerService;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/customers")]
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] UserSearch search)
        {
            try
            {
                var pagedResult = await _customerService.GetCustomersAsync(search, isAdmin: false);
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
                var pagedResult = await _customerService.GetCustomersAsync(search, isAdmin: true);
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
                var result = await _customerService.GetCustomerByIdAsync(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy khách hàng", 404));
                }
                return Ok(ApiResponse<UserDto>.Ok(result));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Lỗi nghiệp vụ khi lấy khách hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<UserDto>.Fail(ex.Message, 400));
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
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            // Validate input: Kiểm tra định dạng ảnh
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

            try
            {
                var entity = await _customerService.UpdateCustomerAsync(id, model, model.Image);
                return Ok(ApiResponse<object>.Ok(entity));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Không tìm thấy khách hàng với ID: {Id}", id);
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Lỗi nghiệp vụ khi cập nhật khách hàng với ID: {Id}", id);
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật khách hàng với ID: {Id}", id);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật khách hàng"));
            }
        }

        [HttpDelete("DeleteCustomer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var result = await _customerService.DeleteCustomerAsync(id);
                return Ok(ApiResponse<bool>.Ok(result));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Không tìm thấy khách hàng với ID: {Id}", id);
                return NotFound(ApiResponse<bool>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa khách hàng với ID: {Id}", id);
                return BadRequest(ApiResponse<bool>.Fail("Có lỗi xảy ra khi xóa khách hàng"));
            }
        }

        /// <summary>
        /// Tạo tài khoản mới cho Customer (Manager)
        /// </summary>
        [Authorize(Roles = "Manager,Admin")]
        [HttpPost("CreateCustomerAccount")]
        public async Task<IActionResult> CreateCustomerAccount([FromForm] CreateCustomerAccountVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var message = await _customerService.CreateCustomerAccountAsync(model, model.image);
                return Ok(ApiResponse<object>.Ok(message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Lỗi nghiệp vụ khi tạo tài khoản customer");
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo tài khoản customer");
                return BadRequest(ApiResponse<object>.Fail($"Có lỗi xảy ra khi tạo tài khoản: {ex.Message}", 400));
            }
        }
    }
}
