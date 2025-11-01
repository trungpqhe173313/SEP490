using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.Dto
{
    public class ProductOrder
    {
        public int ProductId { get; set; }
        public decimal? Quantity { get; set; }
    }
}
