using System;
using System.ComponentModel.DataAnnotations;

namespace NB.Service.TransactionService.ViewModels
{
    public class ApproveImportVM
    {
        [Required(ErrorMessage = "ApproverId không được để trống")]
        public int ApproverId { get; set; }

        [Required(ErrorMessage = "ExpireDate không được để trống")]
        public DateTime ExpireDate { get; set; }
    }
}
