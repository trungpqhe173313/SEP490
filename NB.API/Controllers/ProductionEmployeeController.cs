using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Dto;
using NB.Service.ProductionOrderService;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.ProductionOrderService.ViewModels;
using NB.Service.UserService;
using System.Security.Claims;

namespace NB.API.Controllers
{
    [Route("api/production-employee")]
    [ApiController]
    [Authorize(Roles = "Employee")]
    public class ProductionEmployeeController : ControllerBase
    {
        private readonly IProductionOrderService _productionOrderService;
        private readonly IUserService _userService;
        private readonly ILogger<ProductionEmployeeController> _logger;

        public ProductionEmployeeController(
            IProductionOrderService productionOrderService,
            IUserService userService,
            ILogger<ProductionEmployeeController> logger)
        {
            _productionOrderService = productionOrderService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách các lệnh sản xuất do nhân viên phụ trách
        /// </summary>
        [HttpPost("my-production-orders")]
        public async Task<IActionResult> GetMyProductionOrders([FromBody] ProductionOrderSearch search)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Không thể xác định người dùng", 401));
                }

                // Kiểm tra user có tồn tại không
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin người dùng", 404));
                }

                // Khởi tạo search nếu null
                search ??= new ProductionOrderSearch();

                // Gọi service xử lý logic
                var result = await _productionOrderService.GetDataByResponsibleId(userId, search);

                // Thêm StatusName cho mỗi item
                foreach (var item in result.Items)
                {
                    if (item.Status.HasValue)
                    {
                        ProductionOrderStatus status = (ProductionOrderStatus)item.Status.Value;
                        item.StatusName = status.GetDescription();
                    }
                }

                return Ok(ApiResponse<PagedList<ProductionOrderDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách lệnh sản xuất");
                return BadRequest(ApiResponse<PagedList<ProductionOrderDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu: " + ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách các lệnh sản xuất do một nhân viên cụ thể phụ trách (dành cho Manager)
        /// </summary>
        [HttpPost("production-orders-by-employee/{employeeId}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetProductionOrdersByEmployee([FromRoute] int employeeId, [FromBody] ProductionOrderSearch search)
        {
            try
            {
                // Kiểm tra nhân viên có tồn tại không
                var employee = await _userService.GetByIdAsync(employeeId);
                if (employee == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin nhân viên", 404));
                }

                // Khởi tạo search nếu null
                search ??= new ProductionOrderSearch();

                // Gọi service xử lý logic
                var result = await _productionOrderService.GetDataByResponsibleId(employeeId, search);

                // Thêm StatusName cho mỗi item
                foreach (var item in result.Items)
                {
                    if (item.Status.HasValue)
                    {
                        ProductionOrderStatus status = (ProductionOrderStatus)item.Status.Value;
                        item.StatusName = status.GetDescription();
                    }
                }

                return Ok(ApiResponse<PagedList<ProductionOrderDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách lệnh sản xuất của nhân viên {employeeId}");
                return BadRequest(ApiResponse<PagedList<ProductionOrderDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu: " + ex.Message));
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn sản xuất
        /// </summary>
        [HttpGet("GetDetail/{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            try
            {
                // Validate id
                if (id <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Id không hợp lệ", 400));
                }

                // Lấy UserId từ Claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Không thể xác định người dùng", 401));
                }

                // Kiểm tra xem đơn sản xuất có do nhân viên này phụ trách không
                var productionOrder = await _productionOrderService.GetByIdAsync(id);
                if (productionOrder == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy đơn sản xuất", 404));
                }

                if (productionOrder.ResponsibleId != userId)
                {
                    return Forbid(); // 403 - Nhân viên không có quyền xem đơn này
                }

                // Gọi service để lấy chi tiết
                var result = await _productionOrderService.GetDetailById(id);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đơn sản xuất {id}");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy dữ liệu: " + ex.Message));
            }
        }

        /// <summary>
        /// Chuyển đơn sản xuất sang trạng thái đang xử lý
        /// </summary>
        [HttpPut("ChangeToProcessing/{id}")]
        public async Task<IActionResult> ChangeToProcessing(int id, [FromBody] ChangeToProcessingRequest request)
        {
            try
            {
                // Validate id
                if (id <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Id không hợp lệ", 400));
                }

                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.DeviceCode))
                {
                    return BadRequest(ApiResponse<object>.Fail("DeviceCode là bắt buộc", 400));
                }

                // Lấy UserId từ Claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Không thể xác định người dùng", 401));
                }

                // Gọi service xử lý logic
                var result = await _productionOrderService.ChangeToProcessingAsync(id, request, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đơn sản xuất {id} sang trạng thái đang xử lý");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra: " + ex.Message));
            }
        }

        /// <summary>
        /// Gửi đơn sản xuất để phê duyệt (cập nhật số lượng thành phẩm)
        /// </summary>
        [HttpPut("SubmitForApproval/{id}")]
        public async Task<IActionResult> SubmitForApproval(int id, [FromBody] SubmitForApprovalRequest request)
        {
            try
            {
                // Validate id
                if (id <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Id không hợp lệ", 400));
                }

                // Lấy UserId từ Claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Không thể xác định người dùng", 401));
                }

                // Gọi service xử lý logic
                var result = await _productionOrderService.SubmitForApprovalAsync(id, request, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi đơn sản xuất {id} để phê duyệt");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra: " + ex.Message));
            }
        }
    }
}
