using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class StockAdjustment
{
    public int AdjustmentId { get; set; }

    public int WarehouseId { get; set; }

    public int Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = new List<StockAdjustmentDetail>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
