using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Contract
{
    public int ContractId { get; set; }

    public int? UserId { get; set; }

    public int? SupplierId { get; set; }

    public string? Image { get; set; }

    public string? Pdf { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual User? User { get; set; }
}
