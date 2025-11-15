using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public class TransactionEditVM
    {
        public List<ProductOrder> ListProductOrder { get; set; } = new();
        public string? Note { get; set; }

        public int? Status { get; set; }

        public decimal? TotalCost { get; set; }
    }
}
