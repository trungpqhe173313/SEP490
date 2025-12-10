using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;


namespace NB.Service.ProductService.ViewModels
{
    public class ProductCreateVM
    {
        public string? code { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        public int categoryId { get; set; }

        [Required(ErrorMessage = "Nhà cung cấp là bắt buộc")]
        public int supplierId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được trống")]
        public string productName { get; set; } = null!;

        public string? description { get; set; }

        public IFormFile? image { get; set; }

        [Required(ErrorMessage = "Trọng lượng trên đơn vị là bắt buộc")]
        public decimal weightPerUnit { get; set; }

        [Required(ErrorMessage = "Giá bán là bắt buộc")]
        public decimal sellingPrice { get; set; }
    }
}
