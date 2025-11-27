using System.ComponentModel.DataAnnotations;

namespace NB.Service.JobService.Dto
{
    public class CreateJobDto
    {
        [Required(ErrorMessage = "JobName là bắt buộc")]
        [StringLength(200, ErrorMessage = "JobName không được vượt quá 200 ký tự")]
        public string JobName { get; set; } = null!;

        [Required(ErrorMessage = "PayType là bắt buộc")]
        [RegularExpression("^(Per_Ngay|Per_Tan)$", ErrorMessage = "PayType phải là 'Per_Ngay' hoặc 'Per_Tan'")]
        public string PayType { get; set; } = null!;

        [Required(ErrorMessage = "Rate là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Rate phải >= 0")]
        public decimal Rate { get; set; }
    }
}
