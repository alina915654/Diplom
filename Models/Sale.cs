using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Sale
{
    public int SaleId { get; set; }

    public string CheckNumber { get; set; } = null!;

    public DateTime SaleDate { get; set; }

    public int? ClientId { get; set; }

    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public int PaymentMethod { get; set; }

    public int StatusId { get; set; }

    public int TypeId { get; set; }

    public virtual Client? Client { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<SalesDetail> SalesDetails { get; set; } = new List<SalesDetail>();

    public virtual OrderStatus Status { get; set; } = null!;

    public virtual SaleType Type { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
