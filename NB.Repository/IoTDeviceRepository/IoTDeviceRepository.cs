using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.IoTDeviceRepository
{
    public class IoTDeviceRepository : Repository<IoTdevice>, IIoTDeviceRepository
    {
        public IoTDeviceRepository(DbContext context) : base(context)
        {
        }
    }
}
