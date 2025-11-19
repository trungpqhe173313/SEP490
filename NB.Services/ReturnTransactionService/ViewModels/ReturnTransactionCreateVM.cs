using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionService.ViewModels
{
    public class ReturnTransactionCreateVM
    {
        public int TransactionId { get; set; }

        public string? Reason { get; set; }
    }
}
