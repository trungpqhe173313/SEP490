using NB.Service.SupplierService.ViewModels;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.UserService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public class FullTransactionExportVM
    {
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }

        public string WarehouseName { get; set; }
        public int? Status { get; set; }
        public decimal? TotalCost { get; set; }
        public int? PriceListId { get; set; }
        public int? ResponsibleId { get; set; }
        public string? ResponsibleName { get; set; }
        public CustomerOutputVM Customer { get; set; }
        public List<TransactionDetailOutputVM?> list { get; set; }
    }
}
