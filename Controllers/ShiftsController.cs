using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers
{
    [Authorize(Roles = "Администратор,Кассир,Менеджер")]
    public class ShiftsController : Controller
    {
        private readonly DiplomDbContext _context;

        public ShiftsController(DiplomDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Смены";
            ViewData["Subtitle"] = "Контроль открытых и закрытых смен, кассовые остатки и загрузка сотрудников.";

            var today = DateOnly.FromDateTime(DateTime.Today);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var todayShifts = await _context.WorkShifts
                .AsNoTracking()
                .Where(s => s.ShiftDate == today)
                .Select(s => new
                {
                    s.CashStart,
                    s.CashEnd,
                    s.EndTime,
                    s.ShiftStatus
                })
                .ToListAsync();

            var monthShiftRows = await _context.WorkShifts
                .AsNoTracking()
                .Where(s => s.ShiftDate >= monthStart && s.ShiftDate < nextMonthStart)
                .Select(s => new
                {
                    EmployeeName = s.User.Fio,
                    RoleName = s.User.Role.RoleName,
                    CashEnd = s.CashEnd
                })
                .ToListAsync();

            var model = new ShiftsIndexViewModel
            {
                OpenShifts = await _context.WorkShifts
                    .AsNoTracking()
                    .CountAsync(s => s.EndTime == null || s.ShiftStatus == "Открыта"),
                TodayShifts = todayShifts.Count,
                ClosedToday = todayShifts.Count(s => s.EndTime != null && s.ShiftStatus != "Открыта"),
                OpeningCashToday = todayShifts.Sum(s => s.CashStart ?? 0m),
                ClosingCashToday = todayShifts.Sum(s => s.CashEnd ?? 0m),
                Shifts = await _context.WorkShifts
                    .AsNoTracking()
                    .OrderByDescending(s => s.ShiftDate)
                    .ThenByDescending(s => s.StartTime)
                    .Take(20)
                    .Select(s => new ShiftListItemViewModel
                    {
                        EmployeeName = s.User.Fio,
                        RoleName = s.User.Role.RoleName,
                        ShiftDate = s.ShiftDate,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        CashStart = s.CashStart ?? 0m,
                        CashEnd = s.CashEnd,
                        Status = s.ShiftStatus
                    })
                    .ToListAsync(),
                Employees = monthShiftRows
                    .GroupBy(s => new { s.EmployeeName, s.RoleName })
                    .Select(g => new ShiftEmployeeSummaryItemViewModel
                    {
                        EmployeeName = g.Key.EmployeeName,
                        RoleName = g.Key.RoleName,
                        ShiftCount = g.Count(),
                        TotalCashEnd = g.Sum(x => x.CashEnd ?? 0m)
                    })
                    .OrderByDescending(x => x.ShiftCount)
                    .ThenByDescending(x => x.TotalCashEnd)
                    .Take(8)
                    .ToList()
            };

            return View(model);
        }
    }
}
