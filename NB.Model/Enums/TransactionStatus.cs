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
        @checked,

        //chuyển kho
        [Description("Đang Chuyển")]
        inTransit,
        [Description("Đã Chuyển")]
        transferred,

        [Description("Thanh Toán Tất")]
        paidInFull,
        [Description("Thanh Toán Một Phần")]
        partiallyPaid
    }
}
