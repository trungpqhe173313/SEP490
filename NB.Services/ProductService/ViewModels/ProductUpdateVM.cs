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

        [Required(ErrorMessage = "Số lượng sản phẩm là bắt buộc")]
        public int? StockQuantity { get; set; }
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        public string Code { get; set; } = null!;
        [Required(ErrorMessage = "Mã danh mục là bắt buộc")]
        public int CategoryId { get; set; }
        public int? IsAvailable { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string ProductName { get; set; } = null!;
        public decimal? WeightPerUnit { get; set; }
        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        public decimal? Price { get; set; }

        public DateTime? UpdatedAt { get; set; }

    }
}
