using System.ComponentModel.DataAnnotations;

namespace NB.Service.StockAdjustmentService.ViewModels
{
    public class StockAdjustmentDraftCreateVM
    {
        [Required(ErrorMessage = "WarehouseId là bắt buộc")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Details là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm")]
        public List<StockAdjustmentDetailItemVM> Details { get; set; } = new List<StockAdjustmentDetailItemVM>();
    }

    public class StockAdjustmentDetailItemVM
    {
        [Required(ErrorMessage = "ProductId là bắt buộc")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "ActualQuantity là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "ActualQuantity phải >= 0")]
        public decimal ActualQuantity { get; set; }

        // SystemQuantity: KHÔNG bắt buộc, chỉ để tham khảo khi chỉnh sửa
        // Giá trị này KHÔNG được lưu vào DB
        // Khi GET draft, sẽ lấy lại từ Inventory REALTIME
        public decimal? SystemQuantity { get; set; }
        
        public string? Note { get; set; }
    }
}

