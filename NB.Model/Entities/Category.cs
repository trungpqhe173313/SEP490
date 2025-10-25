﻿using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
