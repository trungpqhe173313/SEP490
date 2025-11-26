using NB.Service.TransactionDetailService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public class FullTransactionTransferVM
    {
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public int? Status { get; set; }
        public string SourceWarehouseName { get; set; }
        public string DestinationWarehouseName { get; set; }
        public decimal? TotalWeight { get; set; }
        public string? Note { get; set; }
        public List<TransactionDetailOutputVM?> list { get; set; }
    }
}

