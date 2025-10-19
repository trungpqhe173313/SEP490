using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class CustomerProductPrice
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public decimal LastPrice { get; set; }

    public DateTime LastOrderDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
