using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService.Dto
{
    public class UserSearch : SearchBase
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool? IsActive { get; set; }
    }
}
