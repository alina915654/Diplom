using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Client
{
    public int ClientId { get; set; }

    public string? Fio { get; set; }

    public string Phone { get; set; } = null!;

    public DateOnly? BirthDate { get; set; }

    public int DiscountPercent { get; set; }

    public decimal TotalSpent { get; set; }

    public int? LinkedUserId { get; set; }

    public virtual ICollection<DiscountCoupon> DiscountCoupons { get; set; } = new List<DiscountCoupon>();

    public virtual User? LinkedUser { get; set; }

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
