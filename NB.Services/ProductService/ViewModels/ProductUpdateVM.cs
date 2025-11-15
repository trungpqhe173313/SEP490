using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;


namespace NB.Service.ProductService.ViewModels
{
    public class ProductUpdateVM
    {
        [Required(ErrorMessage = "Mã nhà kho là bắt buộc")]
        public int warehouseId { get; set; }
        [Required(ErrorMessage = "Mã danh mục là bắt buộc")]
        public string code { get; set; } = null!;
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string productName { get; set; } = null!;
        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        public int supplierId { get; set; }
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        public int categoryId { get; set; }
        public string description { get; set; } = null!;
        public bool? isAvailable { get; set; }
        public IFormFile? image { get; set; }
        public decimal? weightPerUnit { get; set; }

        public decimal? sellingPrice { get; set; }
        public DateTime? updatedAt { get; set; }
    }
}
