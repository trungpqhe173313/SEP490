using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.ViewModels
{
    public class CreateContractVM
    {
        public int? UserId { get; set; }

        public int? SupplierId { get; set; }

        public IFormFile? Image { get; set; }

    }
}
