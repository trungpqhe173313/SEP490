using NB.Service.Dto;

namespace NB.Service.StockAdjustmentService.Dto
{
    public class StockAdjustmentSearch : SearchBase
    {
        public int? WarehouseId { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

