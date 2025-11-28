using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class UpdateWorklogBatchDto
    {
        [Required(ErrorMessage = "EmployeeId là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "WorkDate là bắt buộc")]
        public DateTime WorkDate { get; set; }

        /// <summary>
        /// List công việc cần update
        /// - Nếu null hoặc empty: Xóa tất cả worklog trong ngày (chỉ xóa IsActive = false)
        /// - Nếu có data: So sánh với worklog hiện tại để CREATE/UPDATE/DELETE
        /// </summary>
        public List<WorklogJobItemDto>? Jobs { get; set; }
    }
}
