using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public DateTime? HireDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Salary> Salaries { get; set; } = new List<Salary>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
