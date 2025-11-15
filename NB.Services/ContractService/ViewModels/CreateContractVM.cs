using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.ViewModels
{
    public class CreateContractVM
    {
        [BindProperty(Name = "userId")]
        public int? UserId { get; set; }
        [BindProperty(Name = "supplierId")]
        public int? SupplierId { get; set; }
        [BindProperty(Name = "image")]
        public IFormFile? Image { get; set; }

    }
}
