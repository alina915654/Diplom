using System;
using System.Collections.Generic;

namespace Diplom.ViewModels
{
    public class HomeDashboardViewModel
    {
        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;
        public bool IsAdministrator { get; set; }

        public int TotalEmployees { get; set; }
        public int TotalClients { get; set; }
        public int ActiveProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OpenShifts { get; set; }
        public int OrdersToday { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal AverageCheckToday { get; set; }
        public int OrdersThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int PendingDeliveries { get; set; }
        public int PurchaseOrdersInProgress { get; set; }
        public int MonthlyWasteQuantity { get; set; }
        public int ExpiringItemsCount { get; set; }

        public string BestEmployeeName { get; set; } = "Нет данных";
        public string BestEmployeeRole { get; set; } = string.Empty;
        public int BestEmployeeSalesCount { get; set; }
        public decimal BestEmployeeRevenue { get; set; }

        public List<RecentSaleItemViewModel> RecentSales { get; set; } = new();
        public List<StockAlertItemViewModel> LowStockItems { get; set; } = new();
        public List<ProductPopularityItemViewModel> TopProducts { get; set; } = new();
        public List<WarehouseBalanceItemViewModel> WarehouseBalances { get; set; } = new();
        public List<EmployeePerformanceItemViewModel> EmployeePerformance { get; set; } = new();
        public List<DailySalesChartItemViewModel> DailySalesChart { get; set; } = new();
        public List<SalesTypeChartItemViewModel> SalesByType { get; set; } = new();
        public List<ExpiringInventoryItemViewModel> ExpiringItems { get; set; } = new();
    }

    public class RecentSaleItemViewModel
    {
        public string CheckNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
    }

    public class StockAlertItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int Threshold { get; set; }
    }

    public class ProductPopularityItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public int QuantitySold { get; set; }
    }

    public class WarehouseBalanceItemViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalStock { get; set; }
    }

    public class EmployeePerformanceItemViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DailySalesChartItemViewModel
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
        public double PercentOfMax { get; set; }
    }

    public class SalesTypeChartItemViewModel
    {
        public string TypeName { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
        public double PercentOfTotal { get; set; }
    }

    public class ExpiringInventoryItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public DateOnly ExpirationDate { get; set; }
        public int StockLevel { get; set; }
        public int DaysLeft { get; set; }
    }
}
