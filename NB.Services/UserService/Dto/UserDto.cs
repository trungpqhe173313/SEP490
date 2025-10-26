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
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Image { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? RoleName { get; set; }
        public List<string>? RoleNames { get; set; }
    }
}
