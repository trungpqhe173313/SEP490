using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public class OrderRequest
    {
        public List<ProductOrder> ListProductOrder { get; set; } = new();
        public int? Status { get; set; }
        public string? Note { get; set; }

    }
}
