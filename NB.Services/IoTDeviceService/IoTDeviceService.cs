using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
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
    public class IoTDeviceService : Service<IoTdevice>, IIoTDeviceService
    {
        public IoTDeviceService(IRepository<IoTdevice> repository) : base(repository)
        {
        }

        public async Task<ApiResponse<List<DeviceListDto>>> GetAllDevicesAsync()
        {
            var devices = await GetQueryable()
                .Select(d => new DeviceListDto
                {
                    DeviceCode = d.DeviceCode,
                    DeviceName = d.DeviceName
                })
                .ToListAsync();

            return ApiResponse<List<DeviceListDto>>.Ok(devices);
        }
    }
}
