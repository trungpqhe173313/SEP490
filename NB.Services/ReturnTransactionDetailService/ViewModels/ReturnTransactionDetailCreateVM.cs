using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionDetailService.ViewModels
{
    public class ReturnTransactionDetailCreateVM
    {
        public int ReturnTransactionId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
