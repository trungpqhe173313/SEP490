using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.FinancialTransactionService.Dto
{
    public class FinancialTransactionDto : FinancialTransaction
    {
        public string? TypeName { get; set; }
        public string? CreatedByName { get; set; }
        public int? TypeInt { get; set; }
    }
}
