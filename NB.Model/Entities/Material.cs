using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Material
{
    public int Id { get; set; }

    public int ProductionId { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductionOrder Production { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
