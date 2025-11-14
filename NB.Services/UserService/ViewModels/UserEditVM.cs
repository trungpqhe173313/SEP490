using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NB.Service.UserService.ViewModels
{
    public class UserEditVM
    {
        [AllowNull]
        [BindProperty(Name = "username")]
        public string? Username { get; set; }
        [AllowNull]
        [BindProperty(Name = "email")]
        [EmailAddress]
        public string? Email { get; set; }
        [AllowNull]
        [BindProperty(Name = "password")]
        public string? Password { get; set; }
        [AllowNull]
        [BindProperty(Name = "fullName")]
        public string? FullName { get; set; }
        [AllowNull]
        [BindProperty(Name = "phone")]
        public string? Phone { get; set; }
        [BindProperty(Name = "image")]
        public IFormFile? Image { get; set; }
        [AllowNull]
        [BindProperty(Name = "isActive")]
        public bool? IsActive { get; set; }
    }
}
