using NB.Service.WarehouseService.Dto;
using NB.Service.WarehouseService.ViewModels;

namespace NB.Services.WarehouseService.ViewModels
{
    public class WarehouseImportResultVM
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public List<WarehouseOutputVM> ImportedWarehouses { get; set; } = new List<WarehouseOutputVM>();
    }
}
