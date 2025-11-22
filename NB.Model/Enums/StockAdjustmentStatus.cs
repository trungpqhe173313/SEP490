using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Model.Enums
{
    public enum StockAdjustmentStatus
    {
        [Description("Nháp")]
        Draft = 1,

        [Description("Đã giải quyết")]
        Resolved = 2,

        [Description("Đã hủy")]
        Cancelled = 3
    }
}