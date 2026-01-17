using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionService.Dto
{
    public class ReturnOrderDto
    {
        public int ReturnTransactionId { get; set; }
        public int TransactionId { get; set; }
        public string? Reason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? TransactionDate { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? Status { get; set; }
        public string? TransactionCode { get; set; }
    }
}

