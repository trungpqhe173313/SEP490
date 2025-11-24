using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.InventoryService.Dto
{
    public class ProductInventorySearch
    {
        public int productId { get; set; }
        public int warehouseId { get; set; }
    }
}
