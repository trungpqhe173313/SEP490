using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.ViewModels
{
    public class UpdateContractVM
    {
        public IFormFile? Image { get; set; }

        public bool? IsActive { get; set; }


    }
}
