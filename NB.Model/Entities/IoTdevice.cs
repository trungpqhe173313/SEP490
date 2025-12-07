using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class IoTdevice
{
    public int DeviceId { get; set; }

    public string DeviceCode { get; set; } = null!;

    public string? DeviceName { get; set; }

    public int WarehouseId { get; set; }

    public int? ProductionOrderId { get; set; }

    public decimal RemainingStock { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastSync { get; set; }

    public string? Ipaddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProductionLog> ProductionLogs { get; set; } = new List<ProductionLog>();

    public virtual ProductionOrder? ProductionOrder { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
