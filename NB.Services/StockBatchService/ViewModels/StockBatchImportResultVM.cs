using NB.Service.StockBatchService.ViewModels;

namespace NB.Services.StockBatchService.ViewModels
{
    public class StockBatchImportResultVM
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public List<StockOutputVM> ImportedStockBatches { get; set; } = new List<StockOutputVM>();
    }
}
