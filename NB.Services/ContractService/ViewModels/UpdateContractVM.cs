using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.ViewModels
{
    public class UpdateContractVM
    {
        [BindProperty(Name = "image")]
        public IFormFile? Image { get; set; }
        [BindProperty(Name = "isActive")]
        public bool? IsActive { get; set; }


    }
}
