using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class ViewWarehouseBalance
{
    public string NameCategory { get; set; } = null!;

    public int CategoryId { get; set; }

    public int? TotalStock { get; set; }
}
