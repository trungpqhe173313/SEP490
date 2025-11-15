using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService.ViewModels
{
    public class CustomerOutputVM
    {
        [BindProperty(Name = "userId")]
        public int? UserId { get; set; }
        [BindProperty(Name = "email")]
        public string? Email { get; set; }
        [BindProperty(Name = "fullName")]
        public string? FullName { get; set; }
        [BindProperty(Name = "phone")]
        public string? Phone { get; set; }
        [BindProperty(Name = "image")]
        public string? Image { get; set; }
    }
}
