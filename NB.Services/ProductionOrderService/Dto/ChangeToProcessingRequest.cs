using System.ComponentModel.DataAnnotations;

namespace NB.Service.ProductionOrderService.Dto
{
    /// <summary>
    /// Request cho việc chuyển đơn sản xuất sang trạng thái Processing
    /// </summary>
    public class ChangeToProcessingRequest
    {
        /// <summary>
        /// Mã thiết bị IoT sẽ được gắn với đơn sản xuất này
        /// </summary>
        [Required(ErrorMessage = "DeviceCode là bắt buộc")]
        public string DeviceCode { get; set; } = null!;
    }
}
