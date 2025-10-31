using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService.ViewModels
{
    public class WarehouseCreateVM
    {
        [Required(ErrorMessage = "Tên kho không được để trống.")]
        public string WarehouseName { get; set; } = null!;

        [Required(ErrorMessage = "Vị trí kho không được để trống.")]
        public string Location { get; set; } = null!;

        public int Capacity { get; set; }

        public int Status { get; set; }

        public string? Note { get; set; }
    }
}
