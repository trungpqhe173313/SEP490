using System;

namespace NB.Service.ProductService.Dto
{
    /// <summary>
    /// DTO cho sản phẩm bán chạy
    /// </summary>
    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? WeightPerUnit { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NumberOfOrders { get; set; }
    }
}

