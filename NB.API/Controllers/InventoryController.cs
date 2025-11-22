using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.WarehouseService;

namespace NB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IWarehouseService _warehouseService;
        private readonly IProductService _productService;


        public InventoryController(IInventoryService inventoryService,
            IProductService productService,
            IWarehouseService warehouseService)
        {
            _inventoryService = inventoryService;
            _warehouseService = warehouseService;
            _productService = productService;
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

        /// <summary>
        /// Lấy số lượng sản phẩm tồn kho của sản phẩm trong kho cụ thể
        /// </summary>
        [HttpPost("quantityProduct")]
        public async Task<IActionResult> GetInventoryQuantity([FromBody] ProductInventorySearch search)
        {
            try
            {
                var warehouseExists = await _warehouseService.GetByIdAsync(search.warehouseId);
                if (warehouseExists == null)
                {
                    return NotFound(ApiResponse<int>.Fail("Kho không tồn tại"));
                }

                // Kiểm tra sản phẩm có tồn tại
                var productExists = await _productService.GetByIdAsync(search.productId);
                if (productExists == null)
                {
                    return NotFound(ApiResponse<int>.Fail("Sản phẩm không tồn tại"));
                }

                var quantity = await _inventoryService.GetInventoryQuantity(search.warehouseId, search.productId);
                return Ok(ApiResponse<int>.Ok(quantity));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<int>.Fail("Có lỗi xảy ra khi lấy số lượng tồn kho: " + ex.Message));
            }
        }
    }
}
