using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Recipe
{
    public int RecipeId { get; set; }

    public int ProductId { get; set; }

    public int IngredientId { get; set; }

    public double Amount { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
