using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class CreateWorklogDto
    {
        [Required(ErrorMessage = "EmployeeId là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "JobId là bắt buộc")]
        public int JobId { get; set; }

        /// <summary>
        /// Số lượng (tấn) - Chỉ cần nhập khi PayType = Per_Tan
        /// </summary>
        public decimal? Quantity { get; set; }

        public DateTime? WorkDate { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
}

