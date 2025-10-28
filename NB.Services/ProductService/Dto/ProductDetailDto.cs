using System;

namespace NB.Service.ProductService.Dto
{
    public class ProductDetailDto
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string SupplierName { get; set; } = null!;

        public string CategoryName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public decimal? WeightPerUnit { get; set; }
        public string? Description { get; set; }
        public bool? IsAvailable { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


    }
}
