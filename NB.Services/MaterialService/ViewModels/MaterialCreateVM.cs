using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.MaterialService.ViewModels
{
    public class MaterialCreateVM
    {
        public int ProductionId { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public decimal? TotalWeight { get; set; }
    }
}
