using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.IoTDeviceService;
using System.Threading.Tasks;

namespace NB.API.Controllers
{
    [Route("api/iot-devices")]
    [ApiController]
    [Authorize]
    public class IoTDeviceController : ControllerBase
    {
        private readonly IIoTDeviceService _iotDeviceService;

        public IoTDeviceController(IIoTDeviceService iotDeviceService)
        {
            _iotDeviceService = iotDeviceService;
        }

        /// <summary>
        /// Lấy danh sách tất cả các thiết bị IoT
        /// </summary>
        /// <returns>Danh sách thiết bị IoT với DeviceCode và DeviceName</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDevices()
        {
            var result = await _iotDeviceService.GetAllDevicesAsync();

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result.Data);
        }
    }
}
