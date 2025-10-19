using System;
using System.Collections.Generic;

namespace NB.Model.Entities;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int? IsRead { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual User User { get; set; } = null!;
}
