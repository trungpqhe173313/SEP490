using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService.ViewModels
{
    public class SupplierCreateVM
    {
        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        public string SupplierName { get; set; } = null!;
        [Required(ErrorMessage = "Email nhà cung cấp là bắt buộc")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "SĐT nhà cung cấp là bắt buộc")]
        public string? Phone { get; set; }
    }
}
