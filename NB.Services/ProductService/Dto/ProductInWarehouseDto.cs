using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.Dto
{
    public class ProductInWarehouseDto
    {
        // Thông tin Product
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Code { get; set; } = null!;
        public decimal? Price { get; set; }

        // Thông tin tồn kho (từ Inventory)
        public int QuantityInStock { get; set; }
        public int InventoryId { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
