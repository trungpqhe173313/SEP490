using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionDetailService.ViewModels
{
    public class TransactionDetailCreateVM
    {
        public int TransactionId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? Subtotal { get; set; }
    }
}
