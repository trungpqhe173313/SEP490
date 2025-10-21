using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.ViewModels
{
    public class ProductCreateVM
    {
        public int CategoryId { get; set; }
        public string Code { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; } 

        public int WarehouseId { get; set; }

        public int? InventoryId { get; set; }
    }
}
