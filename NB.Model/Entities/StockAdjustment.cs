using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class StockAdjustment
{
    public int AdjustmentId { get; set; }

    public int WarehouseId { get; set; }

    public int StockBatchId { get; set; }

    public int ProductId { get; set; }

    public DateTime? AdjustmentDate { get; set; }

    public decimal? SystemQuantity { get; set; }

    public decimal? ActualQuantity { get; set; }

    public string? Reason { get; set; }

    public int? CreatedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public int? Status { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual StockBatch StockBatch { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
