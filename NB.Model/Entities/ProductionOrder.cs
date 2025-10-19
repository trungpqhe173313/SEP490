using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class ProductionOrder
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ProductionInput> ProductionInputs { get; set; } = new List<ProductionInput>();

    public virtual ICollection<ProductionOutput> ProductionOutputs { get; set; } = new List<ProductionOutput>();
}
