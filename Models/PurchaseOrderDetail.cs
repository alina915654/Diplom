using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class PurchaseOrderDetail
{
    public int PodetailId { get; set; }

    public int Poid { get; set; }

    public int? ProductId { get; set; }

    public int? IngredientId { get; set; }

    public int OrderedQty { get; set; }

    public int? ReceivedQty { get; set; }

    public decimal UnitCost { get; set; }

    public virtual Ingredient? Ingredient { get; set; }

    public virtual PurchaseOrder Po { get; set; } = null!;

    public virtual Product? Product { get; set; }
}
