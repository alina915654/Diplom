using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class SupplyBatch
{
    public int BatchId { get; set; }

    public int IngredientId { get; set; }

    public int SupplierId { get; set; }

    public DateTime SupplyDate { get; set; }

    public DateOnly ExpirationDate { get; set; }

    public double InitialQuantity { get; set; }

    public double CurrentQuantity { get; set; }

    public decimal PurchasePrice { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
