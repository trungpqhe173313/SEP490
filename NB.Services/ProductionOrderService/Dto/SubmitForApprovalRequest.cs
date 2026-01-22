using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NB.Service.ProductionOrderService.Dto
{
    /// <summary>
    /// Request cho việc gửi đơn sản xuất để phê duyệt
    /// </summary>
    public class SubmitForApprovalRequest
    {
        /// <summary>
        /// Danh sách số lượng thành phẩm sản xuất được
        /// </summary>
        [Required(ErrorMessage = "Danh sách thành phẩm là bắt buộc")]
        public List<FinishProductQuantity> FinishProductQuantities { get; set; } = new List<FinishProductQuantity>();

        /// <summary>
        /// Ghi chú khi gửi đơn để phê duyệt
        /// </summary>
        public string? Note { get; set; }
    }
}
