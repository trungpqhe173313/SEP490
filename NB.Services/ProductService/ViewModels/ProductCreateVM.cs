using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.ViewModels
{
    public class ProductCreateVM
    {
        public int WarehouseId { get; set; }
        public int CategoryId { get; set; }
        public string Code { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int? StockQuantity { get; set; }

        public decimal Weight { get; set; }

        public int SupplierId { get; set; }
    }
}
