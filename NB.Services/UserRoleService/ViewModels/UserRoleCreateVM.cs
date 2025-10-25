using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserRoleService.ViewModels
{
    public class UserRoleCreateVM
    {
        [Required(ErrorMessage ="UserId khôn được để trống")]
        public int UserId { get; set; }
        [Required(ErrorMessage = "RoleId khôn được để trống")]
        public int RoleId { get; set; }

    }
}
