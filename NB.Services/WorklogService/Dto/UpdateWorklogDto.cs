using System.ComponentModel.DataAnnotations;

namespace NB.Service.WorklogService.Dto
{
    public class UpdateWorklogDto
    {
        [Required(ErrorMessage = "Id là bắt buộc")]
        public int Id { get; set; }

        /// <summary>
        /// Số lượng (tấn) - Chỉ cho phép sửa khi PayType = Per_Tan
        /// Nếu PayType = Per_Ngay thì bỏ qua, giữ nguyên Quantity = 1
        /// </summary>
        public decimal? Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
}
