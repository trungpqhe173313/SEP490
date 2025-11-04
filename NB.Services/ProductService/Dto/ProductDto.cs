using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.Dto
{
    public class ProductDto : Product
    {
        
        public decimal? Quantity { get; set; }

        public decimal? AverageCost { get; set; }

        public String? SupplierName { get; set; }

        public String? CategoryName { get; set; }
    }
}
