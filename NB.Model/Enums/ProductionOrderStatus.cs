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
        [Description("Đang chờ xử lý")]
        Pending,
        [Description("Đang xử lý")]
        Processing,
        [Description("Hoàn thành")]
        Finished,
        [Description("Hủy")]
        Cancel,
        [Description("Chờ duyệt")]
        WaitingApproval,
        [Description("Kiểm lại")]
        Rejected,
    }
}
