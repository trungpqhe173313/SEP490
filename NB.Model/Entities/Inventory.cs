using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? AverageCost { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
