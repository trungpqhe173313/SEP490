using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService.Dto
{
    public class SupplierSearch : SearchBase
    {
        public string? SupplierName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}
