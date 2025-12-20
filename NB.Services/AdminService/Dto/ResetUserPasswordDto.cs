using System.ComponentModel.DataAnnotations;

namespace NB.Service.AdminService.Dto
{
    public class ResetUserPasswordDto
    {
        [Required(ErrorMessage = "UserId là bắt buộc")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự")]
        public string Password { get; set; } = null!;
    }
}
