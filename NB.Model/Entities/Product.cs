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

    public string ImageUrl { get; set; } = null!;

    public decimal? WeightPerUnit { get; set; }

    public decimal? SellingPrice { get; set; }

    public string? Description { get; set; }

    public bool? IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Finishproduct> Finishproducts { get; set; } = new List<Finishproduct>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<PriceListDetail> PriceListDetails { get; set; } = new List<PriceListDetail>();

    public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
}
