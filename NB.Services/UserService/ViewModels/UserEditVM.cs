using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService.ViewModels
{
    public class UserEditVM
    {
        [Required(ErrorMessage = "Mã người dùng là bắt buộc")]
        public int UserId { get; set; }
        [AllowNull]
        public string? Username { get; set; }
        [AllowNull]
        public string? Email { get; set; }
        [AllowNull]
        public string? Password { get; set; }
        [AllowNull]
        public string? FullName { get; set; }
        [AllowNull]
        public string? Image { get; set; }
        [AllowNull]
        public bool? IsActive { get; set; }
    }
}
