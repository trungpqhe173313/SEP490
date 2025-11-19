using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionService.Dto
{
    public class ReturnTransactionDto :ReturnTransaction
    {
        public int TransactionId { get; set; }

        public string? Reason { get; set; }
    }
}
