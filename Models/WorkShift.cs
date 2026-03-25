using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class WorkShift
{
    public int ShiftId { get; set; }

    public int UserId { get; set; }

    public DateOnly ShiftDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? CashStart { get; set; }

    public decimal? CashEnd { get; set; }

    public string ShiftStatus { get; set; } = null!;

    public string? ShiftNote { get; set; }

    public virtual User User { get; set; } = null!;
}
