using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Model.Enums
{
    public enum TransactionStatus
    {
        //xuất kho
        [Description("Nháp")]
        draft = 1,
        [Description("Lên đơn")]
        order,
        [Description("Đang giao")]
        delivering,
        [Description("Hoàn thành")]
        done,
        [Description("Thất bại")]
        failure,
        [Description("Hủy")]
        cancel,
        //nhập kho
        [Description("Đang kiểm")]
        checking,
        [Description("Đã kiểm")]
        @checked
    }
}
