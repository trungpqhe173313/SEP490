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
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        public string FullName { get; set; } = null!;

        public string? Image { get; set; }
    }
}
