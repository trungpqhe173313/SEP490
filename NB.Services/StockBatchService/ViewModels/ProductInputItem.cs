using System;
using System.ComponentModel.DataAnnotations;

namespace NB.Service.StockBatchService.ViewModels
{
    public class ProductInputItem
    {
        [Required(ErrorMessage = "ProductId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId phải lớn hơn 0")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Giá nhập là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }
}
