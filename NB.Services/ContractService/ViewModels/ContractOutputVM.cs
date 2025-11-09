using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.ViewModels
{
    public class ContractOutputVM
    {
        public int ContractId { get; set; }

        public string CustomerName { get; set; }

        public string SupplierName { get; set; }

        public string? Image { get; set; }

        public string? Pdf { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

    }
}
