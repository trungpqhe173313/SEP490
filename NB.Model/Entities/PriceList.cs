using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class PriceList
{
    public int PriceListId { get; set; }

    public string PriceListName { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<PriceListDetail> PriceListDetails { get; set; } = new List<PriceListDetail>();
}
