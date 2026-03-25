using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class WasteManagement
{
    public int WasteId { get; set; }

    public int ProductId { get; set; }

    public string? Reason { get; set; }

    public int Quantity { get; set; }

    public DateTime? WasteDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
