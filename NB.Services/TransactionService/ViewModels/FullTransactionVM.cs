using NB.Service.Common;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.SupplierService.Dto;
using NB.Service.SupplierService.ViewModels;
using NB.Service.TransactionDetailService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public class FullTransactionVM
    {
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        
        public string WarehouseName { get; set; }
        public SupplierOutputVM Supplier { get; set; }
        public List<TransactionDetailOutputVM?> list { get; set; }
    }
}
