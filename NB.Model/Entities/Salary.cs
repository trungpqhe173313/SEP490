using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Salary
{
    public int SalaryId { get; set; }

    public int EmployeeId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? PaymentPeriod { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
