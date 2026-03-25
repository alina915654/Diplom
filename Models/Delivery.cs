using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Delivery
{
    public int DeliveryId { get; set; }

    public int SaleId { get; set; }

    public int CourierId { get; set; }

    public string DeliveryAddress { get; set; } = null!;

    public DateTime? DeliveryTime { get; set; }

    public bool IsCompleted { get; set; }

    public virtual User Courier { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
