using System.Security.Claims;
using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers;

[Authorize(Roles = "Администратор")]
public class EmployeesController : Controller
{
    private readonly DiplomDbContext _context;

    public EmployeesController(DiplomDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int? roleId, string status = "all")
    {
        return View(await BuildIndexViewModelAsync(searchTerm, roleId, status));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeEditorViewModel input, string? filterSearchTerm, int? filterRoleId, string filterStatus = "all")
    {
        NormalizeEmployeeInput(input);
        await ValidateEmployeeEditorAsync(input, isEdit: false);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(filterSearchTerm, filterRoleId, filterStatus, input));
        }

        var employee = new User
        {
            Fio = input.FullName,
            Login = input.Login,
            Password = input.Password!,
            RoleId = input.RoleId,
            Phone = input.Phone,
            Email = input.Email,
            Address = input.Address,
            BirthDate = input.BirthDate,
            HireDate = input.HireDate,
            DismissalDate = input.DismissalDate
        };

        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Сотрудник «{employee.Fio}» добавлен.";
        return RedirectToAction(nameof(Index), new { searchTerm = filterSearchTerm, roleId = filterRoleId, status = filterStatus });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(EmployeeEditorViewModel input, string? filterSearchTerm, int? filterRoleId, string filterStatus = "all")
    {
        NormalizeEmployeeInput(input);

        if (!input.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "Не удалось определить сотрудника для обновления.";
            return RedirectToAction(nameof(Index), new { searchTerm = filterSearchTerm, roleId = filterRoleId, status = filterStatus });
        }

        var employee = await _context.Users.FirstOrDefaultAsync(u => u.UserId == input.UserId.Value);
        if (employee is null)
        {
            TempData["ErrorMessage"] = "Сотрудник не найден.";
            return RedirectToAction(nameof(Index), new { searchTerm = filterSearchTerm, roleId = filterRoleId, status = filterStatus });
        }

        await ValidateEmployeeEditorAsync(input, isEdit: true);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(filterSearchTerm, filterRoleId, filterStatus, input));
        }

        employee.Fio = input.FullName;
        employee.Login = input.Login;
        employee.RoleId = input.RoleId;
        employee.Phone = input.Phone;
        employee.Email = input.Email;
        employee.Address = input.Address;
        employee.BirthDate = input.BirthDate;
        employee.HireDate = input.HireDate;
        employee.DismissalDate = input.DismissalDate;

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            employee.Password = input.Password;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Данные сотрудника «{employee.Fio}» обновлены.";
        return RedirectToAction(nameof(Index), new { searchTerm = filterSearchTerm, roleId = filterRoleId, status = filterStatus });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? searchTerm, int? roleId, string status = "all")
    {
        var employee = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (employee is null)
        {
            TempData["ErrorMessage"] = "Сотрудник не найден.";
            return RedirectToAction(nameof(Index), new { searchTerm, roleId, status });
        }

        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == employee.UserId)
        {
            TempData["WarningMessage"] = "Нельзя удалить или деактивировать текущую учетную запись.";
            return RedirectToAction(nameof(Index), new { searchTerm, roleId, status });
        }

        var hasDependencies =
            await _context.Sales.AsNoTracking().AnyAsync(s => s.UserId == employee.UserId) ||
            await _context.WorkShifts.AsNoTracking().AnyAsync(s => s.UserId == employee.UserId) ||
            await _context.Deliveries.AsNoTracking().AnyAsync(d => d.CourierId == employee.UserId) ||
            await _context.Clients.AsNoTracking().AnyAsync(c => c.LinkedUserId == employee.UserId);

        if (hasDependencies)
        {
            employee.DismissalDate ??= DateOnly.FromDateTime(DateTime.Today);
            await _context.SaveChangesAsync();

            TempData["WarningMessage"] = $"Сотрудник «{employee.Fio}» не удален физически из-за связанных данных. Карточка переведена в неактивный статус.";
            return RedirectToAction(nameof(Index), new { searchTerm, roleId, status });
        }

        _context.Users.Remove(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Сотрудник «{employee.Fio}» удален.";
        return RedirectToAction(nameof(Index), new { searchTerm, roleId, status });
    }

    private async Task<EmployeesIndexViewModel> BuildIndexViewModelAsync(
        string? searchTerm,
        int? roleId,
        string? status,
        EmployeeEditorViewModel? editor = null)
    {
        var normalizedStatus = NormalizeStatus(status);
        var trimmedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var today = DateOnly.FromDateTime(DateTime.Today);

        ViewData["Title"] = "Сотрудники";
        ViewData["Subtitle"] = "Управление кадровым составом, ролями, фильтрами и статусами сотрудников.";

        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleName)
            .Select(r => new LookupItemViewModel
            {
                Id = r.RoleId,
                Name = r.RoleName
            })
            .ToListAsync();

        var usersQuery = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trimmedSearch))
        {
            usersQuery = usersQuery.Where(u =>
                u.Fio.Contains(trimmedSearch) ||
                u.Login.Contains(trimmedSearch) ||
                u.Phone.Contains(trimmedSearch) ||
                (u.Email != null && u.Email.Contains(trimmedSearch)));
        }

        if (roleId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.RoleId == roleId.Value);
        }

        usersQuery = normalizedStatus switch
        {
            "active" => usersQuery.Where(u => u.DismissalDate == null || u.DismissalDate > today),
            "dismissed" => usersQuery.Where(u => u.DismissalDate != null && u.DismissalDate <= today),
            _ => usersQuery
        };

        var filteredUsers = await usersQuery
            .OrderBy(u => u.Fio)
            .ToListAsync();

        var allUsers = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .ToListAsync();

        return new EmployeesIndexViewModel
        {
            TotalEmployees = allUsers.Count,
            ActiveEmployees = allUsers.Count(u => u.DismissalDate is null || u.DismissalDate > today),
            DismissedEmployees = allUsers.Count(u => u.DismissalDate is not null && u.DismissalDate <= today),
            HiredThisYear = allUsers.Count(u => u.HireDate is not null && u.HireDate.Value.Year == today.Year),
            SearchTerm = trimmedSearch,
            RoleId = roleId,
            Status = normalizedStatus,
            Roles = roles,
            ByRole = allUsers
                .GroupBy(u => u.Role?.RoleName ?? "Без роли")
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .Select(g => new EmployeeRoleGroupItemViewModel
                {
                    RoleName = g.Key,
                    Count = g.Count()
                })
                .ToList(),
            Employees = filteredUsers
                .Select(u => new EmployeeListItemViewModel
                {
                    UserId = u.UserId,
                    FullName = u.Fio,
                    Login = u.Login,
                    RoleId = u.RoleId,
                    RoleName = u.Role?.RoleName ?? "Без роли",
                    Phone = u.Phone,
                    Email = u.Email ?? string.Empty,
                    Address = u.Address ?? string.Empty,
                    BirthDate = u.BirthDate,
                    HireDate = u.HireDate,
                    DismissalDate = u.DismissalDate,
                    IsActive = u.DismissalDate is null || u.DismissalDate > today
                })
                .ToList(),
            Editor = editor ?? new EmployeeEditorViewModel()
        };
    }

    private async Task ValidateEmployeeEditorAsync(EmployeeEditorViewModel input, bool isEdit)
    {
        if (isEdit && !input.UserId.HasValue)
        {
            ModelState.AddModelError(nameof(EmployeeEditorViewModel.UserId), "Не указан идентификатор сотрудника.");
        }

        if (!isEdit && string.IsNullOrWhiteSpace(input.Password))
        {
            ModelState.AddModelError(nameof(EmployeeEditorViewModel.Password), "Для нового сотрудника нужно указать пароль.");
        }

        if (input.HireDate.HasValue && input.DismissalDate.HasValue && input.DismissalDate < input.HireDate)
        {
            ModelState.AddModelError(nameof(EmployeeEditorViewModel.DismissalDate), "Дата увольнения не может быть раньше даты приема.");
        }

        var roleExists = await _context.Roles
            .AsNoTracking()
            .AnyAsync(r => r.RoleId == input.RoleId);

        if (!roleExists)
        {
            ModelState.AddModelError(nameof(EmployeeEditorViewModel.RoleId), "Выбранная роль не найдена.");
        }

        if (string.IsNullOrWhiteSpace(input.Login))
        {
            return;
        }

        var loginExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Login == input.Login && u.UserId != input.UserId);

        if (loginExists)
        {
            ModelState.AddModelError(nameof(EmployeeEditorViewModel.Login), "Пользователь с таким логином уже существует.");
        }
    }

    private static void NormalizeEmployeeInput(EmployeeEditorViewModel input)
    {
        input.FullName = input.FullName?.Trim() ?? string.Empty;
        input.Login = input.Login?.Trim() ?? string.Empty;
        input.Password = string.IsNullOrWhiteSpace(input.Password) ? null : input.Password.Trim();
        input.Phone = input.Phone?.Trim() ?? string.Empty;
        input.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        input.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
    }

    private static string NormalizeStatus(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "dismissed" => "dismissed",
            _ => "all"
        };
}
