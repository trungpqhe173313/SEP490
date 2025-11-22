using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("GetInventoryData")]
        public async Task<IActionResult> GetInventoryData([FromQuery] int? warehouseId)
        {
            try
            {
                List<InventoryDto> result;

                if (warehouseId.HasValue && warehouseId.Value > 0)
                {
                    // Lấy dữ liệu theo ID kho cụ thể
                    result = await _inventoryService.GetByWarehouseId(warehouseId.Value);
                }
                else
                {
                    // Lấy tất cả dữ liệu inventory từ tất cả các kho
                    result = await _inventoryService.GetData();
                }

                return Ok(ApiResponse<List<InventoryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<InventoryDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu kiểm kho: " + ex.Message));
            }
        }
    }
}
