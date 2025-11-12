using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService.ViewModels
{
    public class SupplierOutputVM
    {
        public int? SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string Status { get; set; }
    }
}
