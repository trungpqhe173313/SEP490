using System.ComponentModel;

namespace NB.Model.Enums
{
    public enum FinancialTransactionType
    {
        [Description("Thu tiền khách hàng")]
        ThuTienKhach,
        
        [Description("Thu khác")]
        ThuKhac,

        [Description("Thanh toán lương")]
        ThanhToanLuong,
        
        [Description("Ứng lương")]
        UngLuong,
        
        [Description("Thanh toán nhận hàng")]
        ThanhToanNhanHang,
        
        [Description("Chi khác")]
        ChiKhac,
    }
}
