using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class StockBatch
{
    public int BatchId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public int? TransactionId { get; set; }

    public int? ProductionFinishId { get; set; }

    public string? BatchCode { get; set; }

    public DateTime? ImportDate { get; set; }

    public DateTime? ExpireDate { get; set; }

    public decimal? QuantityIn { get; set; }

    public decimal? QuantityOut { get; set; }

    public decimal? UnitCost { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? LastUpdated { get; set; }

    public bool? IsActive { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Finishproduct? ProductionFinish { get; set; }

    public virtual Transaction? Transaction { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
