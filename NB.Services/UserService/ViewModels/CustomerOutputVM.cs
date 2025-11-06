using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService.ViewModels
{
    public class CustomerOutputVM
    {
        public int? UserId { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }

        public string? Phone { get; set; }

        public string? Image { get; set; }
    }
}
