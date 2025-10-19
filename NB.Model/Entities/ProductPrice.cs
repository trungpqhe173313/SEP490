using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ProductPrice
{
    public int PriceId { get; set; }

    public int ProductId { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
