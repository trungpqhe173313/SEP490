using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ReturnTransactionDetail
{
    public int Id { get; set; }

    public int ReturnTransactionId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ReturnTransaction ReturnTransaction { get; set; } = null!;
}
