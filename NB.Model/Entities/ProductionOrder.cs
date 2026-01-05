using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ProductionOrder
{
    public int Id { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? ResponsibleId { get; set; }

    public virtual ICollection<Finishproduct> Finishproducts { get; set; } = new List<Finishproduct>();

    public virtual ICollection<IoTdevice> IoTdevices { get; set; } = new List<IoTdevice>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<ProductionWeightLog> ProductionWeightLogs { get; set; } = new List<ProductionWeightLog>();
}
