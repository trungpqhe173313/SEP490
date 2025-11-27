using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class GetWorklogsByDateDto
    {
        [Required(ErrorMessage = "WorkDate là bắt buộc")]
        public DateTime WorkDate { get; set; }
    }
}
