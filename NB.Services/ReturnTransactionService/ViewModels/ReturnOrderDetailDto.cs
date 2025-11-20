using NB.Service.UserService.ViewModels;
using NB.Service.SupplierService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NB.Service.ReturnTransactionDetailService.Dto;

namespace NB.Service.ReturnTransactionService.ViewModels
{
    public class ReturnOrderDetailDto
    {
        public int ReturnTransactionId { get; set; }
        public int TransactionId { get; set; }
        public string? TransactionType { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string WarehouseName { get; set; }
        public int? Status { get; set; }
        public CustomerOutputVM? Customer { get; set; }
        public SupplierOutputVM? Supplier { get; set; }
        public List<ReturnOrderDetailItemDto> Items { get; set; } = new();
    }
}

