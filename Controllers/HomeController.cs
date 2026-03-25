using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Diplom.Controllers
{
    public class HomeController : Controller
    {
        private readonly DiplomDbContext _context;

        public HomeController(DiplomDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var model = new HomeDashboardViewModel
            {
                CurrentUserName = User.Identity?.Name ?? "Пользователь",
                CurrentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "Роль не определена",
                IsAdministrator = User.IsInRole("Администратор")
            };

            ViewData["Title"] = "Главная";
            ViewData["Subtitle"] = model.IsAdministrator
                ? "Оперативная аналитика по продажам, складу, доставке и персоналу."
                : "Выберите нужный раздел в боковом меню для начала работы.";

            if (!model.IsAdministrator)
            {
                return View(model);
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var weekStart = today.AddDays(-6);
            var todayDateOnly = DateOnly.FromDateTime(today);
            var warningDateOnly = DateOnly.FromDateTime(today.AddDays(7));

            var salesTodayAmounts = await _context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow)
                .Select(s => s.FinalAmount)
                .ToListAsync();

            var salesMonthAmounts = await _context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextMonthStart)
                .Select(s => s.FinalAmount)
                .ToListAsync();

            model.TotalEmployees = await _context.Users
                .AsNoTracking()
                .CountAsync();

            model.TotalClients = await _context.Clients
                .AsNoTracking()
                .CountAsync();

            model.ActiveProducts = await _context.Products
                .AsNoTracking()
                .CountAsync(p => p.IsActive);

            model.LowStockProducts = await _context.Products
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.StockQuantity <= 5);

            model.OpenShifts = await _context.WorkShifts
                .AsNoTracking()
                .CountAsync(s => s.EndTime == null || s.ShiftStatus == "Открыта");

            model.PendingDeliveries = await _context.Deliveries
                .AsNoTracking()
                .CountAsync(d => !d.IsCompleted);

            model.PurchaseOrdersInProgress = await _context.PurchaseOrders
                .AsNoTracking()
                .CountAsync(p =>
                    p.Status != null &&
                    p.Status != "Received" &&
                    p.Status != "Получен" &&
                    p.Status != "Completed" &&
                    p.Status != "Завершен");

            model.MonthlyWasteQuantity = await _context.WasteManagements
                .AsNoTracking()
                .Where(w => w.WasteDate >= monthStart && w.WasteDate < nextMonthStart)
                .Select(w => (int?)w.Quantity)
                .SumAsync() ?? 0;

            model.OrdersToday = salesTodayAmounts.Count;
            model.RevenueToday = salesTodayAmounts.Sum();
            model.AverageCheckToday = salesTodayAmounts.Count > 0
                ? salesTodayAmounts.Average()
                : 0m;

            model.OrdersThisMonth = salesMonthAmounts.Count;
            model.RevenueThisMonth = salesMonthAmounts.Sum();

            model.RecentSales = await _context.Sales
                .AsNoTracking()
                .OrderByDescending(s => s.SaleDate)
                .Take(7)
                .Select(s => new RecentSaleItemViewModel
                {
                    CheckNumber = s.CheckNumber,
                    SaleDate = s.SaleDate,
                    ClientName = s.Client != null ? (s.Client.Fio ?? "Без клиента") : "Без клиента",
                    CashierName = s.User.Fio,
                    StatusName = s.Status.StatusName,
                    FinalAmount = s.FinalAmount
                })
                .ToListAsync();

            model.LowStockItems = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.StockQuantity <= 5)
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.NameProduct)
                .Take(8)
                .Select(p => new StockAlertItemViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.NameProduct,
                    CategoryName = p.Category.NameCategory,
                    StockQuantity = p.StockQuantity,
                    Threshold = 5
                })
                .ToListAsync();

            model.TopProducts = await _context.ViewProductPopularities
                .AsNoTracking()
                .OrderByDescending(p => p.TotalQuantitySold ?? 0)
                .ThenByDescending(p => p.NumberOfSales ?? 0)
                .Take(6)
                .Select(p => new ProductPopularityItemViewModel
                {
                    ProductName = p.NameProduct,
                    SalesCount = p.NumberOfSales ?? 0,
                    QuantitySold = p.TotalQuantitySold ?? 0
                })
                .ToListAsync();

            model.WarehouseBalances = await _context.ViewWarehouseBalances
                .AsNoTracking()
                .OrderByDescending(w => w.TotalStock ?? 0)
                .Take(6)
                .Select(w => new WarehouseBalanceItemViewModel
                {
                    CategoryName = w.NameCategory,
                    TotalStock = w.TotalStock ?? 0
                })
                .ToListAsync();

            model.EmployeePerformance = await _context.ViewUserSalesPerformances
                .AsNoTracking()
                .OrderByDescending(u => u.TotalFinalAmount ?? 0)
                .Take(8)
                .Select(u => new EmployeePerformanceItemViewModel
                {
                    FullName = u.Fio,
                    RoleName = u.RoleName,
                    SalesCount = u.TotalSales ?? 0,
                    TotalAmount = u.TotalFinalAmount ?? 0m
                })
                .ToListAsync();

            var bestEmployee = model.EmployeePerformance.FirstOrDefault();
            if (bestEmployee is not null)
            {
                model.BestEmployeeName = bestEmployee.FullName;
                model.BestEmployeeRole = bestEmployee.RoleName;
                model.BestEmployeeSalesCount = bestEmployee.SalesCount;
                model.BestEmployeeRevenue = bestEmployee.TotalAmount;
            }

            var expiringRaw = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.StockLevel > 0 && i.ExpirationDate != null)
                .Select(i => new
                {
                    ProductName = i.Product.NameProduct,
                    i.StockLevel,
                    i.ExpirationDate
                })
                .ToListAsync();

            model.ExpiringItems = expiringRaw
                .Where(i =>
                    i.ExpirationDate.HasValue &&
                    i.ExpirationDate.Value >= todayDateOnly &&
                    i.ExpirationDate.Value <= warningDateOnly)
                .OrderBy(i => i.ExpirationDate)
                .Take(6)
                .Select(i => new ExpiringInventoryItemViewModel
                {
                    ProductName = i.ProductName,
                    StockLevel = i.StockLevel,
                    ExpirationDate = i.ExpirationDate!.Value,
                    DaysLeft = i.ExpirationDate.Value.DayNumber - todayDateOnly.DayNumber
                })
                .ToList();

            model.ExpiringItemsCount = model.ExpiringItems.Count;

            var weeklySales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= weekStart && s.SaleDate < tomorrow)
                .Select(s => new
                {
                    s.SaleDate,
                    s.FinalAmount
                })
                .ToListAsync();

            var groupedByDay = weeklySales
                .GroupBy(x => x.SaleDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        OrdersCount = g.Count(),
                        Revenue = g.Sum(x => x.FinalAmount)
                    });

            var dayChart = new List<DailySalesChartItemViewModel>();

            for (var date = weekStart; date <= today; date = date.AddDays(1))
            {
                var ordersCount = 0;
                var revenue = 0m;

                if (groupedByDay.TryGetValue(date, out var dayData))
                {
                    ordersCount = dayData.OrdersCount;
                    revenue = dayData.Revenue;
                }

                dayChart.Add(new DailySalesChartItemViewModel
                {
                    Date = date,
                    DayLabel = date.ToString("dd.MM"),
                    OrdersCount = ordersCount,
                    Revenue = revenue
                });
            }

            var maxRevenue = dayChart.Count > 0 ? dayChart.Max(x => x.Revenue) : 0m;
            if (maxRevenue <= 0m)
            {
                maxRevenue = 1m;
            }

            foreach (var item in dayChart)
            {
                item.PercentOfMax = Math.Round((double)(item.Revenue / maxRevenue * 100m), 2);
            }

            model.DailySalesChart = dayChart;

            var salesByTypeRaw = await _context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextMonthStart)
                .Select(s => new
                {
                    TypeName = s.Type.TypeName,
                    s.FinalAmount
                })
                .ToListAsync();

            var salesByType = salesByTypeRaw
                .GroupBy(x => x.TypeName)
                .Select(g => new SalesTypeChartItemViewModel
                {
                    TypeName = string.IsNullOrWhiteSpace(g.Key) ? "Без типа" : g.Key,
                    OrdersCount = g.Count(),
                    Revenue = g.Sum(x => x.FinalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var totalRevenueByType = salesByType.Sum(x => x.Revenue);
            if (totalRevenueByType <= 0m)
            {
                totalRevenueByType = 1m;
            }

            foreach (var item in salesByType)
            {
                item.PercentOfTotal = Math.Round((double)(item.Revenue / totalRevenueByType * 100m), 2);
            }

            model.SalesByType = salesByType;

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
