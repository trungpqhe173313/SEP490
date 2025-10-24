using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Finishproduct
{
    public int Id { get; set; }

    public int ProductionId { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductionOrder Production { get; set; } = null!;

    public virtual ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
