using NB.Service.PriceListDetailService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListService.ViewModels
{
    public class PriceListCreateVM
    {
        public string PriceListName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PriceListDetailInputVM>? PriceListDetails { get; set; }
    }
}
