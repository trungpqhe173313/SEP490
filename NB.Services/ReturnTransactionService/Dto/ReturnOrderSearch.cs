using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionService.Dto
{
    public class ReturnOrderSearch : SearchBase
    {
        public string? Type { get; set; } // "Import" hoặc "Export"
        public int? TransactionId { get; set; }
        public string? TransactionCode { get; set; }
    }
}

