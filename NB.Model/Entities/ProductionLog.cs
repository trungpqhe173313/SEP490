using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ProductionLog
{
    public int LogId { get; set; }

    public string DeviceCode { get; set; } = null!;

    public int ProductionOrderId { get; set; }

    public int ProductId { get; set; }

    public decimal ExportedWeight { get; set; }

    public decimal RemainingWeight { get; set; }

    public decimal TotalProcessed { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Note { get; set; }

    public virtual IoTdevice DeviceCodeNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;
}
