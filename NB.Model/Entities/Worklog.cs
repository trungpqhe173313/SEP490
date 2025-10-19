using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Worklog
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int JobId { get; set; }

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    public int? TransactionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual JobType Job { get; set; } = null!;

    public virtual Transaction? Transaction { get; set; }
}
