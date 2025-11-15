using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NB.Service.StockBatchService.ViewModels
{
    public class StockBatchCreateWithProductsVM
    {
        [Required(ErrorMessage = "WarehouseId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "WarehouseId phải lớn hơn 0")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "SupplierId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "SupplierId phải lớn hơn 0")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        public DateTime ExpireDate { get; set; }

        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách sản phẩm là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm")]
        public List<ProductInputItem> Products { get; set; } = new List<ProductInputItem>();

        public decimal? TotalCost { get; set; }
    }
}
