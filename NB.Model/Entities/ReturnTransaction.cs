using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ReturnTransaction
{
    public int ReturnTransactionId { get; set; }

    public int TransactionId { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ReturnTransactionDetail> ReturnTransactionDetails { get; set; } = new List<ReturnTransactionDetail>();

    public virtual Transaction Transaction { get; set; } = null!;
}
