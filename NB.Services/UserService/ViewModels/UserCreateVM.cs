using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        [BindProperty(Name = "username")]
        public string Username { get; set; } = null!;
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [BindProperty(Name = "email")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        [BindProperty(Name = "fullName")]
        public string FullName { get; set; } = null!;
        [Required(ErrorMessage = "Số điện thoại người dùng là bắt buộc")]
        [BindProperty(Name = "phone")]
        public string Phone { get; set; }
        [BindProperty(Name = "image")]
        public IFormFile? Image { get; set; }
    }
}
