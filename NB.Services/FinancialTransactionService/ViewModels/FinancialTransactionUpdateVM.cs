using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.FinancialTransactionService.ViewModels
{
    public class FinancialTransactionUpdateVM
    {
        public int? Type { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public string? PaymentMethod { get; set; }
    }
}

