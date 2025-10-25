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
        public int WarehouseId { get; set; }
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        public string Code { get; set; } = null!;
        [Required(ErrorMessage = "Mã danh mục là bắt buộc")]

        public string ImageUrl { get; set; } = null!;

        public string Description { get; set; } = null!;
        public bool? IsAvailable { get; set; }
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string ProductName { get; set; } = null!;
        public decimal? WeightPerUnit { get; set; }
        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]

        public DateTime? UpdatedAt { get; set; }

    }
}
