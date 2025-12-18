using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.IoTDeviceService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.IoTDeviceService
{
    public interface IIoTDeviceService : IService<IoTdevice>
    {
        /// <summary>
        /// Lấy danh sách tất cả các thiết bị IoT
        /// </summary>
        Task<ApiResponse<List<DeviceListDto>>> GetAllDevicesAsync();
    }
}
