using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<ProductionInput> ProductionInputs { get; set; } = new List<ProductionInput>();

    public virtual ICollection<ProductionOutput> ProductionOutputs { get; set; } = new List<ProductionOutput>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
