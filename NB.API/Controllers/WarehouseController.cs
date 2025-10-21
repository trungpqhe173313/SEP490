using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _warehouseService.GetDto(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<WarehouseDto>.Fail("Không tìm thấy kho", 404));
                }
                return Ok(ApiResponse<WarehouseDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy kho với Id: {Id}", id);
                return BadRequest(ApiResponse<WarehouseDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Warehouse warehouse)
        {
            try
            {
                var result = await _warehouseService.Create(warehouse);
                return Ok(ApiResponse<Warehouse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo kho mới");
                return BadRequest(ApiResponse<Warehouse>.Fail("Có lỗi xảy ra khi tạo kho"));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Warehouse warehouse)
        {
            try
            {
                var result = await _warehouseService.Update(id, warehouse);
                return Ok(ApiResponse<Warehouse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật kho với Id: {Id}", id);
                return BadRequest(ApiResponse<Warehouse>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _warehouseService.Delete(id);
                return Ok(ApiResponse<bool>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa kho với Id: {Id}", id);
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }
    }
}
