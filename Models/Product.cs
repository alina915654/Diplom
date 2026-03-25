using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string NameProduct { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? PhotoPath { get; set; }

    public bool IsActive { get; set; }

    public int StockQuantity { get; set; }

    public string? RecipeMethod { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();

    public virtual ICollection<ProductionCalendar> ProductionCalendars { get; set; } = new List<ProductionCalendar>();

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<SalesDetail> SalesDetails { get; set; } = new List<SalesDetail>();

    public virtual ICollection<WarehouseLog> WarehouseLogs { get; set; } = new List<WarehouseLog>();

    public virtual ICollection<WasteManagement> WasteManagements { get; set; } = new List<WasteManagement>();
}
