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

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual Transaction? RelatedTransaction { get; set; }
}
