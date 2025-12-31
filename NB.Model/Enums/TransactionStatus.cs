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
        //xuất kho (Export)
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
        //nhập kho (Import)
        [Description("Đang kiểm")]
        checking,
        [Description("Đã kiểm")]
        @checked,

        //chuyển kho (Transfer)
        [Description("Đang Chuyển")]
        inTransit,
        [Description("Đã Chuyển")]
        transferred,

        //thanh toán (Payment)
        [Description("Đã thanh toán đủ")]
        paidInFull,
        [Description("Thanh toán một phần")]
        partiallyPaid,

        //trạng thái đặc biệt cho nhập kho (Import) - sử dụng cùng giá trị với trạng thái xuất kho (Export)
        [Description("Đã hủy - Import")]
        importCancelled = 0,
        [Description("Đang kiểm - Import")]
        importChecking = 1,      
        [Description("Đã nhận hàng - Import")]
        importReceived = 2,
        [Description("Chờ phê duyệt kho")]
        pendingWarehouseApproval = 4,
    }
}
