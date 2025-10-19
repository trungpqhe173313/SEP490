using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.EmployeeService.ViewModels
{
    public class EmployeeCreateVM
    {
        //[AllowNull]
        //public int? EmployeeId { get; set; }

        [Required(ErrorMessage = "Mã người dùng là bắt buộc")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Tên người là bắt buộc")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "SĐT là bắt buộc")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Ngày thuê là bắt buộc")]
        public DateTime? HireDate { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string? Status { get; set; }
    }
}
