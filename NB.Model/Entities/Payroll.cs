using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Payroll
{
    public int PayrollId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalAmount { get; set; }

    public bool? IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public string? Note { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual User Employee { get; set; } = null!;

    public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();
}
