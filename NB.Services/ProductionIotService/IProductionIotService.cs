using NB.Service.Dto;
using NB.Service.ProductionIotService.Dto;
using System.Threading.Tasks;

namespace NB.Services.ProductionIotService
{
    public interface IProductionIotService
    {
        Task<ApiResponse<CurrentProductionResponseDto>> GetCurrentProductionAsync(string deviceCode);
        
        /// <summary>
        /// Nhận dữ liệu từ cân điện tử IoT (ESP32) khi công nhân nhấn nút OK
        /// </summary>
        Task<ApiResponse<PackageSubmitResponseDto>> SubmitPackageAsync(PackageSubmitRequestDto request);
    }
}
