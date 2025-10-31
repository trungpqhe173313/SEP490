using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NB.Service.UserService.ViewModels
{
    public class UserEditVM
    {
        [AllowNull]
        public string? Username { get; set; }
        [AllowNull]
        [EmailAddress]
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
