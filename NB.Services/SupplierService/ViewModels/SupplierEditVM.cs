using System.ComponentModel.DataAnnotations;

namespace NB.Service.SupplierService.ViewModels
{
    public class SupplierEditVM
    {
        public string? SupplierName { get; set; } = null!;
        [EmailAddress(ErrorMessage = "Email nhà cung cấp không hợp lệ")]
        public string? Email { get; set; } = null!;

        public string? Phone { get; set; }

        public bool? IsActive { get; set; }

    }
}
