using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class CreateWorklogBatchDto
    {
        [Required(ErrorMessage = "EmployeeId là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Jobs là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 công việc")]
        public List<WorklogJobItemDto> Jobs { get; set; } = new List<WorklogJobItemDto>();

        public DateTime? WorkDate { get; set; }
    }

    public class WorklogJobItemDto
    {
        [Required(ErrorMessage = "JobId là bắt buộc")]
        public int JobId { get; set; }

        /// <summary>
        /// Số lượng (tấn) - Chỉ cần nhập khi PayType = Per_Tan
        /// </summary>
        public decimal? Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
}
