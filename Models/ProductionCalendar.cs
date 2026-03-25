using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class ProductionCalendar
{
    public int CalendarId { get; set; }

    public int ProductId { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public int PlannedQty { get; set; }

    public int? CompletedQty { get; set; }

    public virtual Product Product { get; set; } = null!;
}
