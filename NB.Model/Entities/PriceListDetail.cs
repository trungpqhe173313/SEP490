using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class PriceListDetail
{
    public int PriceListDetailId { get; set; }

    public int? PriceListId { get; set; }

    public int? ProductId { get; set; }

    public decimal Price { get; set; }

    public string? Note { get; set; }

    public virtual PriceList? PriceList { get; set; }

    public virtual Product? Product { get; set; }
}
