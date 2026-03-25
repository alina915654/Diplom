using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string Name { get; set; } = null!;

    public int UnitId { get; set; }

    public double MinStockLevel { get; set; }

    public string? Description { get; set; }

    public double Proteins { get; set; }

    public double Fats { get; set; }

    public double Carbohydrates { get; set; }

    public double Calories { get; set; }

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<SupplyBatch> SupplyBatches { get; set; } = new List<SupplyBatch>();

    public virtual Unit Unit { get; set; } = null!;
}
