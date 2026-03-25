using Diplom.Models;
using Diplom.Services;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers;

[Authorize(Roles = "Администратор,Менеджер")]
public class ReportsController : Controller
{
    private readonly DiplomDbContext _context;
    private readonly BackOfficeExportService _exportService;

    public ReportsController(DiplomDbContext context, BackOfficeExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildIndexViewModelAsync());
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var model = await BuildIndexViewModelAsync();
        var bytes = _exportService.CreateReportsExcel(model);
        var fileName = $"reports-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var model = await BuildIndexViewModelAsync();
        var bytes = _exportService.CreateReportsPdf(model);
        var fileName = $"reports-{DateTime.Now:yyyyMMdd-HHmm}.pdf";

        return File(bytes, "application/pdf", fileName);
    }

    private async Task<ReportsIndexViewModel> BuildIndexViewModelAsync()
    {
        ViewData["Title"] = "Отчёты";
        ViewData["Subtitle"] = "Продажи, структура заказов, популярные товары и результативность сотрудников.";

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var monthSalesRaw = await _context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextMonthStart)
            .Select(s => new
            {
                s.CheckNumber,
                s.SaleDate,
                s.FinalAmount,
                TypeName = s.Type.TypeName,
                StatusName = s.Status.StatusName,
                ClientName = s.Client != null ? s.Client.Fio : null,
                CashierName = s.User.Fio
            })
            .ToListAsync();

        var model = new ReportsIndexViewModel
        {
            OrdersToday = monthSalesRaw.Count(s => s.SaleDate >= today && s.SaleDate < tomorrow),
            RevenueToday = monthSalesRaw
                .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow)
                .Sum(s => s.FinalAmount),
            OrdersThisMonth = monthSalesRaw.Count,
            RevenueThisMonth = monthSalesRaw.Sum(s => s.FinalAmount),
            AverageCheckThisMonth = monthSalesRaw.Count > 0
                ? monthSalesRaw.Sum(s => s.FinalAmount) / monthSalesRaw.Count
                : 0m,
            RecentSales = monthSalesRaw
                .OrderByDescending(s => s.SaleDate)
                .Take(10)
                .Select(s => new RecentSaleItemViewModel
                {
                    CheckNumber = s.CheckNumber,
                    SaleDate = s.SaleDate,
                    ClientName = string.IsNullOrWhiteSpace(s.ClientName) ? "Без клиента" : s.ClientName,
                    CashierName = s.CashierName,
                    StatusName = s.StatusName,
                    FinalAmount = s.FinalAmount
                })
                .ToList(),
            StatusSummary = monthSalesRaw
                .GroupBy(s => s.StatusName)
                .Select(g => new StatusSummaryItemViewModel
                {
                    StatusName = g.Key,
                    OrdersCount = g.Count(),
                    TotalAmount = g.Sum(x => x.FinalAmount)
                })
                .OrderByDescending(x => x.OrdersCount)
                .ToList(),
            TopProducts = await _context.ViewProductPopularities
                .AsNoTracking()
                .OrderByDescending(p => p.TotalQuantitySold ?? 0)
                .ThenByDescending(p => p.NumberOfSales ?? 0)
                .Take(8)
                .Select(p => new ProductPopularityItemViewModel
                {
                    ProductName = p.NameProduct,
                    SalesCount = p.NumberOfSales ?? 0,
                    QuantitySold = p.TotalQuantitySold ?? 0
                })
                .ToListAsync(),
            EmployeePerformance = await _context.ViewUserSalesPerformances
                .AsNoTracking()
                .OrderByDescending(u => u.TotalFinalAmount ?? 0)
                .Take(10)
                .Select(u => new EmployeePerformanceItemViewModel
                {
                    FullName = u.Fio,
                    RoleName = u.RoleName,
                    SalesCount = u.TotalSales ?? 0,
                    TotalAmount = u.TotalFinalAmount ?? 0m
                })
                .ToListAsync()
        };

        var dailyChart = monthSalesRaw
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesChartItemViewModel
            {
                Date = g.Key,
                DayLabel = g.Key.ToString("dd.MM"),
                OrdersCount = g.Count(),
                Revenue = g.Sum(x => x.FinalAmount)
            })
            .ToList();

        var maxRevenue = dailyChart.Count > 0 ? dailyChart.Max(x => x.Revenue) : 0m;
        if (maxRevenue <= 0m)
        {
            maxRevenue = 1m;
        }

        foreach (var item in dailyChart)
        {
            item.PercentOfMax = Math.Round((double)(item.Revenue / maxRevenue * 100m), 2);
        }

        model.DailySalesChart = dailyChart;

        var salesByType = monthSalesRaw
            .GroupBy(s => s.TypeName)
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

        return model;
    }
}
