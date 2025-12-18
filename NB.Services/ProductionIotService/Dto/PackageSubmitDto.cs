using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace NB.Service.ProductionIotService.Dto
{
    /// <summary>
    /// Request DTO từ thiết bị ESP32 khi công nhân nhấn nút OK (chốt 1 bao)
    /// </summary>
    public class PackageSubmitRequestDto
    {
        [Required(ErrorMessage = "Device code is required")]
        public string DeviceCode { get; set; } = null!;

        [Required(ErrorMessage = "Production ID is required")]
        public int ProductionId { get; set; }

        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Weight is required")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Weight must be greater than 0")]
        public decimal Weight { get; set; }   
    }

    /// <summary>
    /// Response DTO sau khi lưu thành công
    /// </summary>
    public class PackageSubmitResponseDto
    {
        public int ProductionId { get; set; }
        public int ProductId { get; set; }
        public int BagIndex { get; set; }
        public decimal ActualWeight { get; set; }
        public decimal TargetWeight { get; set; }
    }
}
