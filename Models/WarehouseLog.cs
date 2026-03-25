using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class WarehouseLog
{
    public int LogId { get; set; }

    public int? ProductId { get; set; }

    public string ActionType { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? Note { get; set; }

    public virtual Product? Product { get; set; }
}
