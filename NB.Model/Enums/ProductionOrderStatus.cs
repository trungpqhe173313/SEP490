using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Model.Enums
{
    public enum ProductionOrderStatus
    {
        [Description("Đàng chờ xử lý")]
        Pending,
        [Description("Đàng xử lý")]
        Processing,
        [Description("Hoàn thành")]
        Finished
    }
}
