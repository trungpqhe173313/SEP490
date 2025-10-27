using System.ComponentModel.DataAnnotations;

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
