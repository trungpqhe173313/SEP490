using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.Dto
{
    public class FinishProductQuantity
    {
        public int ProductId { get; set; }
        public int? Quantity { get; set; } // Nếu null thì dùng số lượng mặc định
    }
}

