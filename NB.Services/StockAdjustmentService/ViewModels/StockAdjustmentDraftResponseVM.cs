namespace NB.Service.StockAdjustmentService.ViewModels
{
    public class StockAdjustmentDraftResponseVM
    {
        public int AdjustmentId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public List<StockAdjustmentDetailResponseVM> Details { get; set; } = new List<StockAdjustmentDetailResponseVM>();
    }

    public class StockAdjustmentDetailResponseVM
    {
        public int DetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ActualQuantity { get; set; }
        public decimal SystemQuantity { get; set; } // REALTIME từ Inventory
        public decimal Difference { get; set; } // = ActualQuantity - SystemQuantity (động)
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

