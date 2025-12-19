using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.ProductionIotService.Dto;
using NB.Services.ProductionIotService;
using System.Linq;
using System.Threading.Tasks;

namespace NB.API.Controllers
{
    [Route("api/iot")]
    [ApiController]
    [Authorize]
    public class ProductionIotController : ControllerBase
    {
        private readonly IProductionIotService _productionIotService;

        public ProductionIotController(IProductionIotService productionIotService)
        {
            _productionIotService = productionIotService;
        }

        /// <summary>
        /// Lấy thông tin lệnh sản xuất hiện tại của một thiết bị IoT
        /// </summary>
        /// <param name="deviceCode">Mã thiết bị (ví dụ: SCALE_01)</param>
        /// <returns>Thông tin lệnh sản xuất và danh sách sản phẩm</returns>
        [HttpGet("production/current")]
        public async Task<IActionResult> GetCurrentProduction([FromQuery] string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                return BadRequest(new { message = "Device code is required" });
            }

            var result = await _productionIotService.GetCurrentProductionAsync(deviceCode);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Nhận dữ liệu từ cân điện tử IoT (ESP32) khi công nhân nhấn nút OK (chốt 1 bao)
        /// </summary>
        /// <param name="request">Dữ liệu từ ESP32: deviceCode, productionId, productId, weight</param>
        /// <returns>Thông tin bao đã chốt: bagIndex, actualWeight, targetWeight</returns>
        [HttpPost("packages")]
        public async Task<IActionResult> SubmitPackage([FromBody] PackageSubmitRequestDto request)
        {
            // Validation từ ModelState (Data Annotations)
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _productionIotService.SubmitPackageAsync(request);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Error?.Message ?? "An error occurred"
                });
            }

            return StatusCode(201, new
            {
                success = true,
                message = "Package submitted successfully",
                data = result.Data
            });
        }
    }
}
