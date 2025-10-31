using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService.Dto
{
    public class UserDto : User
    {
        public string? RoleName { get; set; }
        public List<string>? RoleNames { get; set; }
    }
}
