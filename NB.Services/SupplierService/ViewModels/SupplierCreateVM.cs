using System.ComponentModel.DataAnnotations;

namespace NB.Service.SupplierService.ViewModels
{
    public class SupplierCreateVM
    {
        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        public string SupplierName { get; set; } = null!;
        [Required(ErrorMessage = "Email nhà cung cấp là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email nhà cung cấp không hợp lệ")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "SĐT nhà cung cấp là bắt buộc")]
        public string Phone { get; set; } = null!;
    }
}
