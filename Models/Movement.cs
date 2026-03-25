using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Movement
{
    public int MovementId { get; set; }

    public int ProductId { get; set; }

    public string? MovementType { get; set; }

    public int Quantity { get; set; }

    public DateTime? MovementDate { get; set; }

    public string? Reference { get; set; }

    public virtual Product Product { get; set; } = null!;
}
