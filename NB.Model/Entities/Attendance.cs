using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    public decimal DaysWorked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
