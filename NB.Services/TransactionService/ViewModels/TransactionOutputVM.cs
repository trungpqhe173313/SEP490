using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public  class TransactionOutputVM
    {
        public int? TransactionId { get; set; }

        public int? CustomerId { get; set; }

        public string? WarehouseName{ get; set; }

        public string? SupplierName { get; set; }

        public string? Status { get; set; }
        public string Type { get; set; }
        public DateTime? TransactionDate { get; set; }

        public string? Note { get; set; }
    }
}
