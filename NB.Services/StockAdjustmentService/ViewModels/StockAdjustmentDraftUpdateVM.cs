using System.ComponentModel.DataAnnotations;

namespace NB.Service.StockAdjustmentService.ViewModels
{
    /// <summary>
    /// ViewModel để update phiếu kiểm kho nháp
    /// Logic đơn giản: Xóa hết details cũ, thêm mới toàn bộ
    /// </summary>
    public class StockAdjustmentDraftUpdateVM
    {
        [Required(ErrorMessage = "Details là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm")]
        public List<StockAdjustmentDetailItemVM> Details { get; set; } = new List<StockAdjustmentDetailItemVM>();
    }
}

