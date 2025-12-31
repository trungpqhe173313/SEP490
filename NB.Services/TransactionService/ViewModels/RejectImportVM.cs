using System.ComponentModel.DataAnnotations;

namespace NB.Service.TransactionService.ViewModels
{
    public class RejectImportVM
    {
        [Required(ErrorMessage = "ApproverId không được để trống")]
        public int ApproverId { get; set; }

        [Required(ErrorMessage = "Lý do từ chối không được để trống")]
        public string Reason { get; set; } = string.Empty;
    }
}
