using System.ComponentModel.DataAnnotations;

namespace NB.Service.ProductionOrderService.Dto
{
    /// <summary>
    /// Request cho việc từ chối đơn sản xuất
    /// </summary>
    public class ChangeToRejectedRequest
    {
        /// <summary>
        /// Lý do từ chối (bắt buộc)
        /// </summary>
        [Required(ErrorMessage = "Lý do từ chối là bắt buộc")]
        public string Note { get; set; } = null!;
    }
}
