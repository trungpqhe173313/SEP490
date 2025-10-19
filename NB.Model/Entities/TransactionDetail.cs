using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class TransactionDetail
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Subtotal { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Transaction Transaction { get; set; } = null!;
}
