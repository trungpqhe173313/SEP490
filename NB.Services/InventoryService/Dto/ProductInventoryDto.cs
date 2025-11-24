namespace NB.Service.InventoryService.Dto
{
    /// <summary>
    /// DTO hiển thị thông tin sản phẩm và số lượng tồn kho
    /// </summary>
    public class ProductInventoryDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }
        
        // Nếu có WarehouseId: Số lượng trong kho đó
        // Nếu không có WarehouseId: Tổng số lượng tất cả kho
        public decimal TotalQuantity { get; set; }
        
        // Thông tin kho (nếu lọc theo kho)
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        
        public DateTime? LastUpdated { get; set; }
    }
}

