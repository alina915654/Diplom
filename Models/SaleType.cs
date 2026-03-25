using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class SaleType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
