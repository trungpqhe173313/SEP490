using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.IoTDeviceService.Dto
{
    /// <summary>
    /// DTO cho danh sách thiết bị IoT
    /// </summary>
    public class DeviceListDto
    {
        public string DeviceCode { get; set; }
        public string DeviceName { get; set; }
    }
}
