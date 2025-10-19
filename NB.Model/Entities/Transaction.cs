using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public int WarehouseId { get; set; }

    public string Type { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();

    public virtual User User { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
