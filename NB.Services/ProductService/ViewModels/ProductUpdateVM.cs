using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.ViewModels
{
    public class ProductUpdateVM
    {
        public string Code { get; set; } = null!;
        [Required(ErrorMessage = "Mã danh mục là bắt buộc")]
        public string ImageUrl { get; set; } = null!;
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string ProductName { get; set; } = null!;
        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        public decimal? Quantity { get; set; }
        public int SupplierId { get; set; }
        public string Description { get; set; } = null!;
        public bool? IsAvailable { get; set; }
        public int CategoryId { get; set; }
        public decimal? WeightPerUnit { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
