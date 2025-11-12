using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public int Capacity { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Finishproduct> Finishproducts { get; set; } = new List<Finishproduct>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();
}
