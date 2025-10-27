using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NB.Model.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;
    [EmailAddress]
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Image { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<CustomerPrice> CustomerPrices { get; set; } = new List<CustomerPrice>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
