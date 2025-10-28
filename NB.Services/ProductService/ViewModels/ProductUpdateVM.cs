using System.ComponentModel.DataAnnotations;


namespace NB.Service.ProductService.ViewModels
{
    public class ProductUpdateVM
    {
        [Required(ErrorMessage = "Mã nhà kho là bắt buộc")]
        public int WarehouseId { get; set; }
        [Required(ErrorMessage = "Mã danh mục là bắt buộc")]
        public string Code { get; set; } = null!;
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string ProductName { get; set; } = null!;
        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        public string SupplierName { get; set; } = null!;
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        public string CategoryName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool? IsAvailable { get; set; }
        public string ImageUrl { get; set; } = null!;
        public decimal? WeightPerUnit { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
