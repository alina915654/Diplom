using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers;

[Authorize(Roles = "Администратор,Менеджер")]
public class SuppliersController : Controller
{
    private readonly DiplomDbContext _context;

    public SuppliersController(DiplomDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm)
    {
        return View(await BuildIndexViewModelAsync(searchTerm));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierEditorViewModel input, string? searchTerm)
    {
        NormalizeInput(input);
        await ValidateSupplierAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, input));
        }

        var supplier = new Supplier
        {
            CompanyName = input.CompanyName,
            ContactPerson = input.ContactPerson,
            Phone = input.Phone,
            Email = input.Email,
            Address = input.Address
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Поставщик «{supplier.CompanyName}» создан.";
        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SupplierEditorViewModel input, string? searchTerm)
    {
        NormalizeInput(input);

        if (!input.SupplierId.HasValue)
        {
            TempData["ErrorMessage"] = "Не удалось определить поставщика для редактирования.";
            return RedirectToAction(nameof(Index), new { searchTerm });
        }

        await ValidateSupplierAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, input));
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == input.SupplierId.Value);
        if (supplier is null)
        {
            TempData["ErrorMessage"] = "Поставщик не найден.";
            return RedirectToAction(nameof(Index), new { searchTerm });
        }

        supplier.CompanyName = input.CompanyName;
        supplier.ContactPerson = input.ContactPerson;
        supplier.Phone = input.Phone;
        supplier.Email = input.Email;
        supplier.Address = input.Address;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Поставщик «{supplier.CompanyName}» обновлен.";
        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? searchTerm)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.PurchaseOrders)
            .Include(s => s.SupplyBatches)
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier is null)
        {
            TempData["ErrorMessage"] = "Поставщик не найден.";
            return RedirectToAction(nameof(Index), new { searchTerm });
        }

        if (supplier.PurchaseOrders.Count > 0 || supplier.SupplyBatches.Count > 0)
        {
            TempData["WarningMessage"] = $"Поставщика «{supplier.CompanyName}» нельзя удалить, так как у него есть связанные закупки или поставки.";
            return RedirectToAction(nameof(Index), new { searchTerm });
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Поставщик «{supplier.CompanyName}» удален.";
        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    private async Task<SuppliersIndexViewModel> BuildIndexViewModelAsync(string? searchTerm, SupplierEditorViewModel? editor = null)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        ViewData["Title"] = "Поставщики";
        ViewData["Subtitle"] = "Управление справочником поставщиков, контактами и связями с закупками.";

        var allSuppliers = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.PurchaseOrders)
            .Include(s => s.SupplyBatches)
            .OrderBy(s => s.CompanyName)
            .ToListAsync();

        IEnumerable<Supplier> filteredSuppliers = allSuppliers;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            filteredSuppliers = filteredSuppliers.Where(s =>
                s.CompanyName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                (s.ContactPerson?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                s.Phone.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                (s.Email?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Address?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return new SuppliersIndexViewModel
        {
            TotalSuppliers = allSuppliers.Count,
            SuppliersWithOrders = allSuppliers.Count(s => s.PurchaseOrders.Count > 0),
            SuppliersWithBatches = allSuppliers.Count(s => s.SupplyBatches.Count > 0),
            FilteredSuppliersCount = filteredSuppliers.Count(),
            SearchTerm = normalizedSearch,
            Suppliers = filteredSuppliers
                .Select(s => new SupplierListItemViewModel
                {
                    SupplierId = s.SupplierId,
                    CompanyName = s.CompanyName,
                    ContactPerson = s.ContactPerson ?? string.Empty,
                    Phone = s.Phone,
                    Email = s.Email ?? string.Empty,
                    Address = s.Address ?? string.Empty,
                    PurchaseOrdersCount = s.PurchaseOrders.Count,
                    SupplyBatchesCount = s.SupplyBatches.Count
                })
                .ToList(),
            Editor = editor ?? new SupplierEditorViewModel()
        };
    }

    private async Task ValidateSupplierAsync(SupplierEditorViewModel input)
    {
        var duplicateNameExists = await _context.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.CompanyName == input.CompanyName && s.SupplierId != input.SupplierId);

        if (duplicateNameExists)
        {
            ModelState.AddModelError(nameof(SupplierEditorViewModel.CompanyName), "Поставщик с таким названием уже существует.");
        }
    }

    private static void NormalizeInput(SupplierEditorViewModel input)
    {
        input.CompanyName = input.CompanyName?.Trim() ?? string.Empty;
        input.ContactPerson = string.IsNullOrWhiteSpace(input.ContactPerson) ? null : input.ContactPerson.Trim();
        input.Phone = input.Phone?.Trim() ?? string.Empty;
        input.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        input.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
    }
}
