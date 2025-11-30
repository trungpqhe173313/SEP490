using NB.Service.Dto;
using System;

namespace NB.Service.FinancialTransactionService.Dto
{
    public class FinancialTransactionSearch : SearchBase
    {
        public string? Type { get; set; }
        public int? RelatedTransactionId { get; set; }
        public int? PayrollId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? TransactionFromDate { get; set; }
        public DateTime? TransactionToDate { get; set; }
    }
}

