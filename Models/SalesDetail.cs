using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class SalesDetail
{
    public int DetailId { get; set; }

    public int SaleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal CostPrice { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
