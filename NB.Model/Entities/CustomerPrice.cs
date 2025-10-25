using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class CustomerPrice
{
    public int CustomerPriceId { get; set; }

    public int? CustomerId { get; set; }

    public int? ProductId { get; set; }

    public decimal? Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual User? Customer { get; set; }

    public virtual Product? Product { get; set; }
}
