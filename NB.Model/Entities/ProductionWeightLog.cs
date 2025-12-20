using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ProductionWeightLog
{
    public int LogId { get; set; }

    public int ProductionId { get; set; }

    public int ProductId { get; set; }

    public string DeviceCode { get; set; } = null!;

    public decimal ActualWeight { get; set; }

    public decimal TargetWeight { get; set; }

    public int BagIndex { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual IoTdevice DeviceCodeNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductionOrder Production { get; set; } = null!;
}
