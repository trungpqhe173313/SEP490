using System.ComponentModel.DataAnnotations;

namespace NB.Service.TransactionService.ViewModels
{
    public class UpdateToCheckedStatusRequest
    {
        public int ResponsibleId { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        public DateTime ExpireDate { get; set; }
    }
}
