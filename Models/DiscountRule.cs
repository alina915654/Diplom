using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class DiscountRule
{
    public int RuleId { get; set; }

    public string Title { get; set; } = null!;

    public decimal MinSpending { get; set; }

    public int DiscountValue { get; set; }
}
