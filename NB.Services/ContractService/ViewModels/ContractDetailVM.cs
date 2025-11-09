using NB.Service.SupplierService.ViewModels;
using NB.Service.UserService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.ViewModels
{
    public class ContractDetailVM
    {
        public int ContractId { get; set; }

        public string? Image { get; set; }

        public string? Pdf { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public CustomerOutputVM Customer { get; set; }

        public SupplierOutputVM Supplier { get; set; }
    }
}
