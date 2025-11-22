using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.ViewModels
{
    public class ProductionOrderCreateVM
    {
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? Status { get; set; }

        public string? Note { get; set; }
    }
}
