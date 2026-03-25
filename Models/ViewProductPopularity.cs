using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class ViewProductPopularity
{
    public int ProductId { get; set; }

    public string NameProduct { get; set; } = null!;

    public int? NumberOfSales { get; set; }

    public int? TotalQuantitySold { get; set; }
}
