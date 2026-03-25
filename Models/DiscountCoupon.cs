using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class DiscountCoupon
{
    public int CouponId { get; set; }

    public string Code { get; set; } = null!;

    public double DiscountPercentage { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public int? UsedByClientId { get; set; }

    public virtual Client? UsedByClient { get; set; }
}
