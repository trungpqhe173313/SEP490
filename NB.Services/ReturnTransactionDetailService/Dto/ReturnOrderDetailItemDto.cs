using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionDetailService.Dto
{
    public class ReturnOrderDetailItemDto
    {
        public int ReturnTransactionDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}

