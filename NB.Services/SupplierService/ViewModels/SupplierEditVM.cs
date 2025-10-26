using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService.ViewModels
{
    public class SupplierEditVM
    {
        public string? SupplierName { get; set; } = null!;

        public string? Email { get; set; } = null!;

        public string? Phone { get; set; }

        public bool? IsActive { get; set; }

    }
}
