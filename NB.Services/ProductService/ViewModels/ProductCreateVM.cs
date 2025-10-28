using System.ComponentModel.DataAnnotations;


namespace NB.Service.ProductService.ViewModels
{
    public class ProductCreateVM
    {
        [Required(ErrorMessage = "Mã nhà kho là bắt buộc")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Mã sản phẩm không được trống")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        public int CategoryId { get; set; } = null!;

        [Required(ErrorMessage = "Nhà cung cấp là bắt buộc")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được trống")]
        public string ProductName { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "URL hình ảnh là bắt buộc")]
        public string ImageUrl { get; set; } = null!;

        [Required(ErrorMessage = "Trọng lượng trên đơn vị là bắt buộc")]
        public decimal WeightPerUnit { get; set; }
    }
}
