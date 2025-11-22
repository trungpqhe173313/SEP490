using System.Collections.Generic;

namespace NB.Service.ProductService.ViewModels
{
    public class ProductImportResultVM
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public List<ProductImportedItemVM> ImportedProducts { get; set; } = new List<ProductImportedItemVM>();
    }

    public class ProductImportedItemVM
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal? WeightPerUnit { get; set; }
        public decimal? SellingPrice { get; set; }
        public string? Description { get; set; }
    }
}
