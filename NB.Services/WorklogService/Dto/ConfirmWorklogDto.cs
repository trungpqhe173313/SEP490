using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class ConfirmWorklogDto
    {
        [Required(ErrorMessage = "EmployeeId là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "WorkDate là bắt buộc")]
        public DateTime WorkDate { get; set; }
    }
}
