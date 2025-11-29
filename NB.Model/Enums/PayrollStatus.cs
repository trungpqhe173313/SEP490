using System.ComponentModel;

namespace NB.Model.Enums
{
    public enum PayrollStatus
    {
        [Description("Chưa tạo bảng lương")]
        NotGenerated,
        
        [Description("Đã tạo bảng lương")]
        Generated,
        
        [Description("Đã thanh toán")]
        Paid
    }
}
