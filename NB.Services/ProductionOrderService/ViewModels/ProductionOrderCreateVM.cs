using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.ViewModels
{
    public class ProductionOrderCreateVM
    {
        public int ResponsibleId { get; set; }
        public string? Note { get; set; }
    }
}
