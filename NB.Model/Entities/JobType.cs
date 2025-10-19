using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class JobType
{
    public int Id { get; set; }

    public string JobName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal Rate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
