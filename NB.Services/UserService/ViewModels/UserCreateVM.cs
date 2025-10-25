using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService.ViewModels
{
    public class UserCreateVM
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        public string Username { get; set; } = null!;
        [Required(ErrorMessage = "Email là bắt buộc")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Password là bắt buộc")]
        public string Password { get; set; } = null!;
        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        public string FullName { get; set; } = null!;

        public string? Image { get; set; }
    }
}
