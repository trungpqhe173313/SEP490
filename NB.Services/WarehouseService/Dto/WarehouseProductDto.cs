using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.WarehouseRepository.Dto
{
    public class WarehouseProductDto
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string? Unit { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? IsAvailable { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public string WarehouseLocation { get; set; } = null!;
        public int? Quantity { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
