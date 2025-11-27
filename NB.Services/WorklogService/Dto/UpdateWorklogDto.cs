using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class UpdateWorklogDto
    {
        [Required(ErrorMessage = "EmployeeId là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "WorkDate là bắt buộc")]
        public DateTime WorkDate { get; set; }

        [Required(ErrorMessage = "JobId là bắt buộc")]
        public int JobId { get; set; }

        /// <summary>
        /// Số lượng (tấn) - Chỉ cho phép sửa khi PayType = Per_Tan
        /// Nếu PayType = Per_Ngay thì bỏ qua, giữ nguyên Quantity = 1
        /// </summary>
        public decimal? Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
}
