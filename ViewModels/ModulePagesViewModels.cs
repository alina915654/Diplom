using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Diplom.ViewModels;

public class LookupItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class EmployeesIndexViewModel
{
    public int TotalEmployees { get; set; }

    public int ActiveEmployees { get; set; }

    public int DismissedEmployees { get; set; }

    public int HiredThisYear { get; set; }

    public string? SearchTerm { get; set; }

    public int? RoleId { get; set; }

    public string Status { get; set; } = "all";

    public List<LookupItemViewModel> Roles { get; set; } = [];

    public List<EmployeeRoleGroupItemViewModel> ByRole { get; set; } = [];

    public List<EmployeeListItemViewModel> Employees { get; set; } = [];

    public EmployeeEditorViewModel Editor { get; set; } = new();
}

public class EmployeeRoleGroupItemViewModel
{
    public string RoleName { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class EmployeeListItemViewModel
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public DateOnly? HireDate { get; set; }

    public DateOnly? DismissalDate { get; set; }

    public bool IsActive { get; set; }
}

public class EmployeeEditorViewModel
{
    public int? UserId { get; set; }

    [Required(ErrorMessage = "Укажите ФИО сотрудника.")]
    [StringLength(100, ErrorMessage = "ФИО должно содержать не более 100 символов.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите логин.")]
    [StringLength(50, ErrorMessage = "Логин должен содержать не более 50 символов.")]
    public string Login { get; set; } = string.Empty;

    [StringLength(50, MinimumLength = 4, ErrorMessage = "Пароль должен содержать от 4 до 50 символов.")]
    public string? Password { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите роль.")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Укажите телефон.")]
    [StringLength(20, ErrorMessage = "Телефон должен содержать не более 20 символов.")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [StringLength(100, ErrorMessage = "Email должен содержать не более 100 символов.")]
    public string? Email { get; set; }

    [StringLength(255, ErrorMessage = "Адрес должен содержать не более 255 символов.")]
    public string? Address { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? HireDate { get; set; }

    public DateOnly? DismissalDate { get; set; }
}

public class ReportsIndexViewModel
{
    public int OrdersToday { get; set; }

    public decimal RevenueToday { get; set; }

    public int OrdersThisMonth { get; set; }

    public decimal RevenueThisMonth { get; set; }

    public decimal AverageCheckThisMonth { get; set; }

    public List<DailySalesChartItemViewModel> DailySalesChart { get; set; } = [];

    public List<SalesTypeChartItemViewModel> SalesByType { get; set; } = [];

    public List<StatusSummaryItemViewModel> StatusSummary { get; set; } = [];

    public List<ProductPopularityItemViewModel> TopProducts { get; set; } = [];

    public List<EmployeePerformanceItemViewModel> EmployeePerformance { get; set; } = [];

    public List<RecentSaleItemViewModel> RecentSales { get; set; } = [];
}

public class StatusSummaryItemViewModel
{
    public string StatusName { get; set; } = string.Empty;

    public int OrdersCount { get; set; }

    public decimal TotalAmount { get; set; }
}

public class ShiftsIndexViewModel
{
    public int OpenShifts { get; set; }

    public int TodayShifts { get; set; }

    public int ClosedToday { get; set; }

    public decimal OpeningCashToday { get; set; }

    public decimal ClosingCashToday { get; set; }

    public List<ShiftListItemViewModel> Shifts { get; set; } = [];

    public List<ShiftEmployeeSummaryItemViewModel> Employees { get; set; } = [];
}

public class ShiftListItemViewModel
{
    public string EmployeeName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public DateOnly ShiftDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal CashStart { get; set; }

    public decimal? CashEnd { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class ShiftEmployeeSummaryItemViewModel
{
    public string EmployeeName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public int ShiftCount { get; set; }

    public decimal TotalCashEnd { get; set; }
}

public class WarehouseIndexViewModel
{
    public int TotalStockUnits { get; set; }

    public int LowStockProducts { get; set; }

    public int ExpiringItemsCount { get; set; }

    public int WasteThisMonth { get; set; }

    public int OpenPurchaseOrders { get; set; }

    public string? SearchTerm { get; set; }

    public int? CategoryId { get; set; }

    public string StockState { get; set; } = "all";

    public List<LookupItemViewModel> Categories { get; set; } = [];

    public List<LookupItemViewModel> Products { get; set; } = [];

    public List<InventoryListItemViewModel> InventoryItems { get; set; } = [];

    public WarehouseInventoryEditorViewModel Editor { get; set; } = new();

    public List<StockAlertItemViewModel> LowStockItems { get; set; } = [];

    public List<ExpiringInventoryItemViewModel> ExpiringItems { get; set; } = [];

    public List<WarehouseBalanceItemViewModel> Balances { get; set; } = [];

    public List<PurchaseOrderListItemViewModel> PurchaseOrders { get; set; } = [];

    public List<WarehouseLogListItemViewModel> Logs { get; set; } = [];
}

public class InventoryListItemViewModel
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int StockLevel { get; set; }

    public int MinStockLevel { get; set; }

    public int ReorderPoint { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public bool IsLowStock { get; set; }

    public bool IsExpiringSoon { get; set; }
}

public class WarehouseInventoryEditorViewModel
{
    public int? InventoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите товар.")]
    public int ProductId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Остаток не может быть отрицательным.")]
    public int StockLevel { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Минимальный остаток не может быть отрицательным.")]
    public int MinStockLevel { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Точка заказа не может быть отрицательной.")]
    public int ReorderPoint { get; set; }

    public DateOnly? ExpirationDate { get; set; }
}

public class PurchaseOrderListItemViewModel
{
    public int Number { get; set; }

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? OrderDate { get; set; }

    public DateTime? ExpectedArrivalDate { get; set; }

    public int ItemsCount { get; set; }

    public decimal TotalAmount { get; set; }

    public bool CanEdit { get; set; } = true;

    public string? EditDisabledReason { get; set; }

    public List<PurchaseOrderDetailListItemViewModel> Details { get; set; } = [];
}

public class WarehouseLogListItemViewModel
{
    public DateTime? Timestamp { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string Note { get; set; } = string.Empty;
}

public class PurchaseOrdersIndexViewModel
{
    public int TotalOrders { get; set; }

    public int OrdersInProgress { get; set; }

    public int OrdersReceived { get; set; }

    public int SuppliersCount { get; set; }

    public string? SearchTerm { get; set; }

    public int? SupplierId { get; set; }

    public string Status { get; set; } = "all";

    public List<LookupItemViewModel> Suppliers { get; set; } = [];

    public List<LookupItemViewModel> Ingredients { get; set; } = [];

    public List<string> StatusOptions { get; set; } = [];

    public List<PurchaseOrderListItemViewModel> PurchaseOrders { get; set; } = [];

    public PurchaseOrderEditorViewModel Editor { get; set; } = new();
}

public class PurchaseOrderEditorViewModel
{
    public int? Poid { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите поставщика.")]
    public int SupplierId { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? ExpectedArrivalDate { get; set; }

    [Required(ErrorMessage = "Укажите статус закупки.")]
    [StringLength(50, ErrorMessage = "Статус должен содержать не более 50 символов.")]
    public string Status { get; set; } = "Draft";

    public List<PurchaseOrderDetailEditorViewModel> Details { get; set; } =
    [
        new PurchaseOrderDetailEditorViewModel()
    ];
}

public class PurchaseOrderDetailEditorViewModel
{
    public int? PodetailId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите ингредиент в составе закупки.")]
    public int IngredientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть больше нуля.")]
    public int OrderedQty { get; set; } = 1;

    [Range(0, int.MaxValue, ErrorMessage = "Принятое количество не может быть отрицательным.")]
    public int? ReceivedQty { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Цена должна быть неотрицательной.")]
    public decimal UnitCost { get; set; }
}

public class PurchaseOrderDetailListItemViewModel
{
    public int? PodetailId { get; set; }

    public int? IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public bool IsLegacyProduct { get; set; }

    public int OrderedQty { get; set; }

    public int? ReceivedQty { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }
}

public class ProductsIndexViewModel
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int TotalCategories { get; set; }

    public int LowStockProducts { get; set; }

    public string? SearchTerm { get; set; }

    public int? CategoryId { get; set; }

    public string ActivityState { get; set; } = "all";

    public List<LookupItemViewModel> Categories { get; set; } = [];

    public List<ProductListItemViewModel> Products { get; set; } = [];

    public List<CategoryListItemViewModel> CategoryItems { get; set; } = [];

    public ProductEditorViewModel ProductEditor { get; set; } = new();

    public CategoryEditorViewModel CategoryEditor { get; set; } = new();
}

public class ProductListItemViewModel
{
    public int ProductId { get; set; }

    public string NameProduct { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public string PhotoPath { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int StockQuantity { get; set; }

    public string RecipeMethod { get; set; } = string.Empty;
}

public class CategoryListItemViewModel
{
    public int CategoryId { get; set; }

    public string NameCategory { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public int ActiveProductCount { get; set; }
}

public class ProductEditorViewModel
{
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Укажите название товара.")]
    [StringLength(100, ErrorMessage = "Название товара должно содержать не более 100 символов.")]
    public string NameProduct { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Выберите категорию.")]
    public int CategoryId { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Цена должна быть неотрицательной.")]
    public decimal Price { get; set; }

    [StringLength(1000, ErrorMessage = "Описание слишком длинное.")]
    public string? Description { get; set; }

    public string? ExistingPhotoPath { get; set; }

    public IFormFile? Photo { get; set; }

    [StringLength(4000, ErrorMessage = "Текст рецепта слишком длинный.")]
    public string? RecipeMethod { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "Остаток не может быть отрицательным.")]
    public int StockQuantity { get; set; }
}

public class CategoryEditorViewModel
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Укажите название категории.")]
    [StringLength(50, ErrorMessage = "Название категории должно содержать не более 50 символов.")]
    public string NameCategory { get; set; } = string.Empty;
}

public class SuppliersIndexViewModel
{
    public int TotalSuppliers { get; set; }

    public int SuppliersWithOrders { get; set; }

    public int SuppliersWithBatches { get; set; }

    public int FilteredSuppliersCount { get; set; }

    public string? SearchTerm { get; set; }

    public List<SupplierListItemViewModel> Suppliers { get; set; } = [];

    public SupplierEditorViewModel Editor { get; set; } = new();
}

public class SupplierListItemViewModel
{
    public int SupplierId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int PurchaseOrdersCount { get; set; }

    public int SupplyBatchesCount { get; set; }
}

public class SupplierEditorViewModel
{
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Укажите название компании.")]
    [StringLength(100, ErrorMessage = "Название компании должно содержать не более 100 символов.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Контактное лицо должно содержать не более 100 символов.")]
    public string? ContactPerson { get; set; }

    [Required(ErrorMessage = "Укажите телефон.")]
    [StringLength(20, ErrorMessage = "Телефон должен содержать не более 20 символов.")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [StringLength(100, ErrorMessage = "Email должен содержать не более 100 символов.")]
    public string? Email { get; set; }

    [StringLength(200, ErrorMessage = "Адрес должен содержать не более 200 символов.")]
    public string? Address { get; set; }
}
