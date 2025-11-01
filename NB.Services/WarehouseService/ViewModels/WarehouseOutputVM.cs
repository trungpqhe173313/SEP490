using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService.ViewModels
{
    public class WarehouseOutputVM
    {
        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = null!;

        public string Location { get; set; } = null!;

        public int Capacity { get; set; }

        public string Status { get; set; }

        public bool? IsActive { get; set; }

        public string? Note { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
