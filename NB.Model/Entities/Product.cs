using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Product
{
    public int ProductId { get; set; }

    public int SupplierId { get; set; }

    public int CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal? WeightPerUnit { get; set; }

    public int? StockQuantity { get; set; }

    public int? IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? Id { get; set; }

    public string? Name { get; set; }

    public string? Unit { get; set; }

    public decimal? Price { get; set; }

    public decimal? Weight { get; set; }

    public int? ProductBaseId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<CustomerProductPrice> CustomerProductPrices { get; set; } = new List<CustomerProductPrice>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    public virtual ICollection<ProductionInput> ProductionInputs { get; set; } = new List<ProductionInput>();

    public virtual ICollection<ProductionOutput> ProductionOutputs { get; set; } = new List<ProductionOutput>();

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
}
