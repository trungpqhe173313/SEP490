using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class FinancialTransaction
{
    public int FinancialTransactionId { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public string? PaymentMethod { get; set; }

    public int? RelatedTransactionId { get; set; }

    public int? CreatedBy { get; set; }

    public int? PayrollId { get; set; }

    public string? ImageUrl { get; set; }

    public int? Status { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual Payroll? Payroll { get; set; }

    public virtual Transaction? RelatedTransaction { get; set; }
}
