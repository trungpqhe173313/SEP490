using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService.Dto
{
    public class UpdateProfileDto
    {
        public string? fullName { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
        public string? image { get; set; }
        public IFormFile? imageFile { get; set; }
    }
}
