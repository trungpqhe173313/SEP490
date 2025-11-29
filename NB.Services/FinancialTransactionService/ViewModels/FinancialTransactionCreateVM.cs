using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.FinancialTransactionService.ViewModels
{
    public class FinancialTransactionCreateVM
    {
        public string? Type { get; set; }
        public decimal? Amount { get; set; }

        public string? Description { get; set; }
        [Required(ErrorMessage ="Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; } = null!;

        public int? CreatedBy { get; set; }
    }
}
