using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public int StockLevel { get; set; }

    public int MinStockLevel { get; set; }

    public int ReorderPoint { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
