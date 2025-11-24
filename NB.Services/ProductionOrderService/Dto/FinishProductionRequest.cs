using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.Dto
{
    public class FinishProductionRequest
    {
        public List<FinishProductQuantity>? FinishProductQuantities { get; set; }
    }
}

