using System.ComponentModel.DataAnnotations;

namespace NB.Service.AdminService.Dto
{
    public class AdminChangePasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;
    }
}