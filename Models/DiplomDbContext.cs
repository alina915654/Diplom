using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Models;

public partial class DiplomDbContext : DbContext
{
    public DiplomDbContext()
    {
    }

    public DiplomDbContext(DbContextOptions<DiplomDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<DiscountCoupon> DiscountCoupons { get; set; }

    public virtual DbSet<DiscountRule> DiscountRules { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Movement> Movements { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductionCalendar> ProductionCalendars { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleType> SaleTypes { get; set; }

    public virtual DbSet<SalesDetail> SalesDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplyBatch> SupplyBatches { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<ViewProductPopularity> ViewProductPopularities { get; set; }

    public virtual DbSet<ViewUserSalesPerformance> ViewUserSalesPerformances { get; set; }

    public virtual DbSet<ViewWarehouseBalance> ViewWarehouseBalances { get; set; }

    public virtual DbSet<WarehouseLog> WarehouseLogs { get; set; }

    public virtual DbSet<WasteManagement> WasteManagements { get; set; }

    public virtual DbSet<WorkShift> WorkShifts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B6B381FBD");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.NameCategory).HasMaxLength(50);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Clients__E67E1A04905D337C");

            entity.HasIndex(e => e.Fio, "IX_Clients_FIO");

            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.Fio)
                .HasMaxLength(100)
                .HasColumnName("FIO");
            entity.Property(e => e.LinkedUserId).HasColumnName("LinkedUserID");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.LinkedUser).WithMany(p => p.Clients)
                .HasForeignKey(d => d.LinkedUserId)
                .HasConstraintName("FK_Clients_User");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__Deliveri__626D8FEEBB4ACE6B");

            entity.Property(e => e.DeliveryId).HasColumnName("DeliveryID");
            entity.Property(e => e.CourierId).HasColumnName("CourierID");
            entity.Property(e => e.DeliveryAddress).HasMaxLength(255);
            entity.Property(e => e.DeliveryTime).HasColumnType("datetime");
            entity.Property(e => e.SaleId).HasColumnName("SaleID");

            entity.HasOne(d => d.Courier).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.CourierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Deliveries_Courier");

            entity.HasOne(d => d.Sale).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Deliveries_Sale");
        });

        modelBuilder.Entity<DiscountCoupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Discount__384AF1DA1D985DB9");

            entity.HasIndex(e => e.Code, "UQ__Discount__A25C5AA74F4E6D28").IsUnique();

            entity.Property(e => e.CouponId).HasColumnName("CouponID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.UsedByClientId).HasColumnName("UsedByClientID");
            entity.Property(e => e.ValidFrom).HasColumnType("datetime");
            entity.Property(e => e.ValidTo).HasColumnType("datetime");

            entity.HasOne(d => d.UsedByClient).WithMany(p => p.DiscountCoupons)
                .HasForeignKey(d => d.UsedByClientId)
                .HasConstraintName("FK_DiscountCoupons_Client");
        });

        modelBuilder.Entity<DiscountRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PK__Discount__110458C26DC3A9F8");

            entity.Property(e => e.RuleId).HasColumnName("RuleID");
            entity.Property(e => e.MinSpending).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Title).HasMaxLength(50);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PK__Ingredie__BEAEB27ADDE8D345");

            entity.Property(e => e.IngredientId).HasColumnName("IngredientID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UnitId).HasColumnName("UnitID");

            entity.HasOne(d => d.Unit).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ingredients_Unit");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__F5FDE6D3934C1880");

            entity.ToTable("Inventory");

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Product");
        });

        modelBuilder.Entity<Movement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK__Movement__D1822466BCB3249B");

            entity.Property(e => e.MovementId).HasColumnName("MovementID");
            entity.Property(e => e.MovementDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MovementType).HasMaxLength(50);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Reference).HasMaxLength(100);

            entity.HasOne(d => d.Product).WithMany(p => p.Movements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movements_Product");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__OrderSta__C8EE20432B032161");

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED7BDFE3B5");

            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryID");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NameProduct).HasMaxLength(100);
            entity.Property(e => e.PhotoPath).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Category");
        });

        modelBuilder.Entity<ProductionCalendar>(entity =>
        {
            entity.HasKey(e => e.CalendarId).HasName("PK__Producti__53CFDBAD7F73001B");

            entity.ToTable("ProductionCalendar");

            entity.Property(e => e.CalendarId).HasColumnName("CalendarID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ScheduledDate).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductionCalendars)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductionCalendar_Product");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Poid).HasName("PK__Purchase__5F02A2F475AB72DD");

            entity.Property(e => e.Poid).HasColumnName("POID");
            entity.Property(e => e.ExpectedArrivalDate).HasColumnType("datetime");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrders_Supplier");
        });

        modelBuilder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.HasKey(e => e.PodetailId).HasName("PK__Purchase__4EB47B5EE6B2E9C9");

            entity.Property(e => e.PodetailId).HasColumnName("PODetailID");
            entity.Property(e => e.IngredientId).HasColumnName("IngredientID");
            entity.Property(e => e.Poid).HasColumnName("POID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.IngredientId)
                .HasConstraintName("FK_PurchaseOrderDetails_Ingredient");

            entity.HasOne(d => d.Po).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.Poid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrderDetails_PO");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_PurchaseOrderDetails_Product");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.RecipeId).HasName("PK__Recipes__FDD988D0E03DDA3F");

            entity.Property(e => e.RecipeId).HasColumnName("RecipeID");
            entity.Property(e => e.IngredientId).HasColumnName("IngredientID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recipes_Ingredient");

            entity.HasOne(d => d.Product).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recipes_Product");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3ADCBFBD02");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("PK__Sales__1EE3C41F0D119B79");

            entity.HasIndex(e => e.ClientId, "IX_Sales_ClientID");

            entity.HasIndex(e => e.SaleDate, "IX_Sales_SaleDate");

            entity.Property(e => e.SaleId).HasColumnName("SaleID");
            entity.Property(e => e.CheckNumber).HasMaxLength(50);
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SaleDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Client).WithMany(p => p.Sales)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("FK_Sales_Client");

            entity.HasOne(d => d.Status).WithMany(p => p.Sales)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sales_Status");

            entity.HasOne(d => d.Type).WithMany(p => p.Sales)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sales_Type");

            entity.HasOne(d => d.User).WithMany(p => p.Sales)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sales_User");
        });

        modelBuilder.Entity<SaleType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__SaleType__516F0395146F4A58");

            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<SalesDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__SalesDet__135C314DAE86F862");

            entity.HasIndex(e => e.ProductId, "IX_SalesDetails_ProductID");

            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SaleId).HasColumnName("SaleID");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesDetails_Product");

            entity.HasOne(d => d.Sale).WithMany(p => p.SalesDetails)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesDetails_Sale");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666943C2D5181");

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<SupplyBatch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("PK__SupplyBa__5D55CE38F68A81A2");

            entity.Property(e => e.BatchId).HasColumnName("BatchID");
            entity.Property(e => e.IngredientId).HasColumnName("IngredientID");
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.SupplyDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.SupplyBatches)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplyBatches_Ingredient");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplyBatches)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplyBatches_Supplier");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__Units__44F5EC95B8C3D2D2");

            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.UnitName).HasMaxLength(10);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACDBC842DF");

            entity.HasIndex(e => e.Login, "UQ__Users__5E55825B343A1EF5").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Fio)
                .HasMaxLength(100)
                .HasColumnName("FIO");
            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Role");
        });

        modelBuilder.Entity<ViewProductPopularity>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("View_ProductPopularity");

            entity.Property(e => e.NameProduct).HasMaxLength(100);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
        });

        modelBuilder.Entity<ViewUserSalesPerformance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("View_UserSalesPerformance");

            entity.Property(e => e.Fio)
                .HasMaxLength(100)
                .HasColumnName("FIO");
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.TotalFinalAmount).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalSalesAmount).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<ViewWarehouseBalance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("View_WarehouseBalance");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.NameCategory).HasMaxLength(50);
        });

        modelBuilder.Entity<WarehouseLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Warehous__5E5499A84EE2542A");

            entity.ToTable("WarehouseLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ActionType).HasMaxLength(10);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.WarehouseLogs)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_WarehouseLog_Product");
        });

        modelBuilder.Entity<WasteManagement>(entity =>
        {
            entity.HasKey(e => e.WasteId).HasName("PK__WasteMan__716E5521308834BB");

            entity.ToTable("WasteManagement");

            entity.Property(e => e.WasteId).HasColumnName("WasteID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Reason).HasMaxLength(100);
            entity.Property(e => e.WasteDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.WasteManagements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WasteManagement_Product");
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__WorkShif__C0A838E127CA701E");

            entity.Property(e => e.ShiftId).HasColumnName("ShiftID");
            entity.Property(e => e.CashEnd).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CashStart).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.ShiftStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Открыта");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.WorkShifts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShifts_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
