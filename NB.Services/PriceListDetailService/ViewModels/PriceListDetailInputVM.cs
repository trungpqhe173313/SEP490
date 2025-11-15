using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListDetailService.ViewModels
{
    public class PriceListDetailInputVM
    {
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
