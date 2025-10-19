using Microsoft.AspNetCore.Mvc;
using NB.Repository.WarehouseRepository.Dto;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.WarehouseService;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    public class WarehouseController : Controller
    {
        private readonly IWarehouseService _warehouseService;
        private readonly ILogger<WarehouseController> _logger;

        public WarehouseController(
            IWarehouseService warehouseService,
            ILogger<WarehouseController> logger)
        {
            _warehouseService = warehouseService;
            _logger = logger;
        }

        [HttpGet("getproducts")]
        public async Task<IActionResult> GetProducts([FromQuery] WarehouseProductSearch search)
        {
            try
            {
                var result = await _warehouseService.GetProducts(search);
                return Ok(ApiResponse<PagedList<WarehouseProductDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm từ kho");
                return BadRequest(ApiResponse<PagedList<WarehouseProductDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }
    }
}
