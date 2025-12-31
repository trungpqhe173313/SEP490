using System;

namespace NB.Service.TransactionService.ViewModels
{
    public class SubmitForApprovalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TransactionId { get; set; }
        public int Status { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class ApproveImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TransactionId { get; set; }
        public int Status { get; set; }
        public int ApprovedBy { get; set; }
        public DateTime ApprovedDate { get; set; }
    }

    public class RejectImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TransactionId { get; set; }
        public int Status { get; set; }
        public int RejectedBy { get; set; }
        public DateTime RejectedDate { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}
