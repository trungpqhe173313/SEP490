using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService.ViewModels
{
    public class WarehouseUpdateVM
    {

        [Required(ErrorMessage = "Tên kho không được để trống.")]
        public string WarehouseName { get; set; } = null!;

        [Required(ErrorMessage = "Vị trí kho không được để trống.")]
        public string Location { get; set; } = null!;

        [Required(ErrorMessage = "Sức chứa kho không được để trống.")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Trạng thái kho không được để trống.")]
        public int Status { get; set; }

        [Required(ErrorMessage = "Ghi chú không được để trống.")]
        public string? Note { get; set; }
    }
}
