namespace NB.Service.StockAdjustmentService.Dto
{
    public class StockAdjustmentListDto
    {
        public int AdjustmentId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int TotalProducts { get; set; }
    }
}

