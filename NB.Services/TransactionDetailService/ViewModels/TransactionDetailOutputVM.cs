using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionDetailService.ViewModels
{
    public class TransactionDetailOutputVM
    {
        public int TransactionDetailId { get; set; }
        public int ProductId { get; set; }
        public string? Code { get; set; }
        public string ProductName { get; set; } = null!;

        public decimal UnitPrice { get; set; }

        public decimal? WeightPerUnit { get; set; }
        public int Quantity { get; set; }


        public string? Note { get; set; }
        public DateTime? ExpireDate { get; set; }
    }
}
