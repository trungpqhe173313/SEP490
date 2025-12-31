using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NB.Service.TransactionService.ViewModels
{
    public class SubmitForApprovalVM
    {
        [Required(ErrorMessage = "ResponsibleId không được để trống")]
        public int ResponsibleId { get; set; }

        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống")]
        public List<ProductActualQuantity> Products { get; set; } = new List<ProductActualQuantity>();
    }

    public class ProductActualQuantity
    {
        [Required(ErrorMessage = "ProductId không được để trống")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "ActualQuantity không được để trống")]
        [Range(0, int.MaxValue, ErrorMessage = "ActualQuantity phải lớn hơn hoặc bằng 0")]
        public int ActualQuantity { get; set; }
    }
}
