using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NB.API.Controllers
{
    [Route("api/analytics")]
    [Authorize(Roles = "Manager")]
    public class AnalyticsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IProductService productService,
            IUserService userService,
            ILogger<AnalyticsController> logger)
        {
            _productService = productService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy top 10 sản phẩm bán chạy nhất trong khoảng thời gian
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu (yyyy-MM-dd)</param>
        /// <param name="toDate">Ngày kết thúc (yyyy-MM-dd)</param>
        /// <returns>Danh sách top 10 sản phẩm bán chạy nhất</returns>
        [HttpGet("GetTopSellingProducts")]
        public async Task<IActionResult> GetTopSellingProducts([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                // Validate input
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    return BadRequest(ApiResponse<List<TopSellingProductDto>>.Fail("Vui lòng cung cấp đầy đủ ngày bắt đầu và ngày kết thúc"));
                }

                if (fromDate.Value > toDate.Value)
                {
                    return BadRequest(ApiResponse<List<TopSellingProductDto>>.Fail("Ngày bắt đầu không được lớn hơn ngày kết thúc"));
                }

                // Gọi service để lấy dữ liệu
                var topProducts = await _productService.GetTopSellingProducts(fromDate.Value, toDate.Value);

                return Ok(ApiResponse<List<TopSellingProductDto>>.Ok(topProducts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy top 10 sản phẩm bán chạy nhất");
                return BadRequest(ApiResponse<List<TopSellingProductDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu phân tích"));
            }
        }

        /// <summary>
        /// Lấy top 10 khách hàng mua hàng nhiều nhất dựa trên tổng tiền đã mua trong khoảng thời gian
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu (yyyy-MM-dd)</param>
        /// <param name="toDate">Ngày kết thúc (yyyy-MM-dd)</param>
        /// <returns>Danh sách top 10 khách hàng mua hàng nhiều nhất</returns>
        [HttpGet("GetTopCustomersByTotalSpent")]
        public async Task<IActionResult> GetTopCustomersByTotalSpent([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                // Validate input
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    return BadRequest(ApiResponse<List<TopCustomerDto>>.Fail("Vui lòng cung cấp đầy đủ ngày bắt đầu và ngày kết thúc"));
                }

                if (fromDate.Value > toDate.Value)
                {
                    return BadRequest(ApiResponse<List<TopCustomerDto>>.Fail("Ngày bắt đầu không được lớn hơn ngày kết thúc"));
                }

                // Gọi service để lấy dữ liệu
                var topCustomers = await _userService.GetTopCustomersByTotalSpent(fromDate.Value, toDate.Value);

                return Ok(ApiResponse<List<TopCustomerDto>>.Ok(topCustomers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy top 10 khách hàng mua hàng nhiều nhất");
                return BadRequest(ApiResponse<List<TopCustomerDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu phân tích"));
            }
        }/// <summary>
        /// Lấy top 10 khách hàng mua hàng nhiều nhất dựa trên tổng tiền đã mua trong khoảng thời gian
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu (yyyy-MM-dd)</param>
        /// <param name="toDate">Ngày kết thúc (yyyy-MM-dd)</param>
        /// <returns>Danh sách top 10 khách hàng mua hàng nhiều nhất</returns>
        [HttpGet("GetCustomerTotalSpending/{userId}")]
        public async Task<IActionResult> GetCustomerTotalSpending(int userId,[FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                // Validate input
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    return BadRequest(ApiResponse<List<TopCustomerDto>>.Fail("Vui lòng cung cấp đầy đủ ngày bắt đầu và ngày kết thúc"));
                }

                if (fromDate.Value > toDate.Value)
                {
                    return BadRequest(ApiResponse<List<TopCustomerDto>>.Fail("Ngày bắt đầu không được lớn hơn ngày kết thúc"));
                }

                var exsitUser = await _userService.GetByIdAsync(userId);
                if (exsitUser == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy người dùng"));
                }

                // Gọi service để lấy dữ liệu
                var customers = await _userService.GetCustomerTotalSpending(userId, fromDate.Value, toDate.Value);

                return Ok(ApiResponse<TopCustomerDto>.Ok(customers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy top 10 khách hàng mua hàng nhiều nhất");
                return BadRequest(ApiResponse<List<TopCustomerDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu phân tích"));
            }
        }
    }
}

