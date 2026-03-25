using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class ViewUserSalesPerformance
{
    public int UserId { get; set; }

    public string Fio { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public int? TotalSales { get; set; }

    public decimal? TotalSalesAmount { get; set; }

    public decimal? TotalFinalAmount { get; set; }
}
