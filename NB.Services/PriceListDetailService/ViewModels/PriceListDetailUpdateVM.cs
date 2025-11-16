using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListDetailService.ViewModels
{
    public class PriceListDetailUpdateVM
    {
        public List<PriceListDetailInputVM> PriceListDetails { get; set; } = new List<PriceListDetailInputVM>();
    }
}
