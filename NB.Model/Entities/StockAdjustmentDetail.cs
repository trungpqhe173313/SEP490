using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class StockAdjustmentDetail
{
    public int DetailId { get; set; }

    public int AdjustmentId { get; set; }

    public int ProductId { get; set; }

    public decimal ActualQuantity { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal SystemQuantity { get; set; }

    public virtual StockAdjustment Adjustment { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
