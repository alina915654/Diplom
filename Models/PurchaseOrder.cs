using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class PurchaseOrder
{
    public int Poid { get; set; }

    public int SupplierId { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? ExpectedArrivalDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual Supplier Supplier { get; set; } = null!;
}
