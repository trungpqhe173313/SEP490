using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListDetailService.ViewModels
{
    public class PriceListDetailOutputVM
    {
        public int PriceListDetailId { get; set; }

        public int? ProductId { get; set; }
        public string? ProductName { get; set; }

        public string? ProductCode { get; set; }
        public decimal Price { get; set; }

        public string? Note { get; set; }
    }
}
