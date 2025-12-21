using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class IoTdevice
{
    public int DeviceId { get; set; }

    public string DeviceCode { get; set; } = null!;

    public string? DeviceName { get; set; }

    public int WarehouseId { get; set; }

    public int? CurrentProductionId { get; set; }

    public bool? IsOnline { get; set; }

    public DateTime? LastHeartbeat { get; set; }

    public DateTime? CreatedAt { get; set; }
}
