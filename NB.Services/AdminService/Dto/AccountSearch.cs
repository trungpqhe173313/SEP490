using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AdminService.Dto
{
    public class AccountSearch : SearchBase
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool? IsActive { get; set; }
    }
}
