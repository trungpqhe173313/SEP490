using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.Dto
{
    public class MaterialDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}

