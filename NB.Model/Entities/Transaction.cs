using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int? CustomerId { get; set; }

    public int? WarehouseInId { get; set; }

    public int? SupplierId { get; set; }

    public int WarehouseId { get; set; }

    public decimal? ConversionRate { get; set; }

    public string Type { get; set; } = null!;

    public int? Status { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();

    public virtual ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();

    public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();

    public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
