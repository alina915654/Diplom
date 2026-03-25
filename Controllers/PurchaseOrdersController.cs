using Diplom.Models;
using Diplom.Services;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers;

[Authorize(Roles = "Администратор,Менеджер")]
public class PurchaseOrdersController : Controller
{
    private static readonly string[] StatusOptions =
    [
        "Черновик",
        "Ожидается",
        "В пути",
        "Получен",
        "Отменен"
    ];

    private readonly DiplomDbContext _context;
    private readonly BackOfficeExportService _exportService;

    public PurchaseOrdersController(DiplomDbContext context, BackOfficeExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int? supplierId, string status = "all")
    {
        return View(await BuildIndexViewModelAsync(searchTerm, supplierId, status));
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? searchTerm, int? supplierId, string status = "all")
    {
        var model = await BuildIndexViewModelAsync(searchTerm, supplierId, status);
        var bytes = _exportService.CreatePurchaseOrdersExcel(model);
        var fileName = $"purchase-orders-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? searchTerm, int? supplierId, string status = "all")
    {
        var model = await BuildIndexViewModelAsync(searchTerm, supplierId, status);
        var bytes = _exportService.CreatePurchaseOrdersPdf(model);
        var fileName = $"purchase-orders-{DateTime.Now:yyyyMMdd-HHmm}.pdf";

        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PurchaseOrderEditorViewModel input,
        string? searchTerm,
        int? supplierId,
        string status = "all")
    {
        NormalizeEditor(input);
        await ValidateEditorAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, supplierId, status, input));
        }

        var purchaseOrder = new PurchaseOrder
        {
            SupplierId = input.SupplierId,
            OrderDate = input.OrderDate ?? DateTime.Now,
            ExpectedArrivalDate = input.ExpectedArrivalDate,
            Status = input.Status
        };

        foreach (var detail in input.Details)
        {
            purchaseOrder.PurchaseOrderDetails.Add(new PurchaseOrderDetail
            {
                IngredientId = detail.IngredientId,
                ProductId = null,
                OrderedQty = detail.OrderedQty,
                ReceivedQty = detail.ReceivedQty,
                UnitCost = detail.UnitCost
            });
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Закупка №{purchaseOrder.Poid} создана.";
        return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        PurchaseOrderEditorViewModel input,
        string? searchTerm,
        int? supplierId,
        string status = "all")
    {
        NormalizeEditor(input);

        if (!input.Poid.HasValue)
        {
            TempData["ErrorMessage"] = "Не удалось определить закупку для редактирования.";
            return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
        }

        await ValidateEditorAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, supplierId, status, input));
        }

        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderDetails)
            .FirstOrDefaultAsync(p => p.Poid == input.Poid.Value);

        if (purchaseOrder is null)
        {
            TempData["ErrorMessage"] = "Закупка не найдена.";
            return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
        }

        purchaseOrder.SupplierId = input.SupplierId;
        purchaseOrder.OrderDate = input.OrderDate ?? purchaseOrder.OrderDate ?? DateTime.Now;
        purchaseOrder.ExpectedArrivalDate = input.ExpectedArrivalDate;
        purchaseOrder.Status = input.Status;

        _context.PurchaseOrderDetails.RemoveRange(purchaseOrder.PurchaseOrderDetails);
        purchaseOrder.PurchaseOrderDetails.Clear();

        foreach (var detail in input.Details)
        {
            purchaseOrder.PurchaseOrderDetails.Add(new PurchaseOrderDetail
            {
                IngredientId = detail.IngredientId,
                ProductId = null,
                OrderedQty = detail.OrderedQty,
                ReceivedQty = detail.ReceivedQty,
                UnitCost = detail.UnitCost
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Закупка №{purchaseOrder.Poid} обновлена.";
        return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? searchTerm, int? supplierId, string status = "all")
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderDetails)
            .FirstOrDefaultAsync(p => p.Poid == id);

        if (purchaseOrder is null)
        {
            TempData["ErrorMessage"] = "Закупка не найдена.";
            return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
        }

        var hasReceivedItems = purchaseOrder.PurchaseOrderDetails.Any(d => (d.ReceivedQty ?? 0) > 0);
        if (hasReceivedItems)
        {
            purchaseOrder.Status = "Отменен";
            await _context.SaveChangesAsync();

            TempData["WarningMessage"] = $"Закупка №{purchaseOrder.Poid} не удалена, так как по ней уже есть принятые позиции. Статус изменен на «Отменен».";
            return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
        }

        _context.PurchaseOrderDetails.RemoveRange(purchaseOrder.PurchaseOrderDetails);
        _context.PurchaseOrders.Remove(purchaseOrder);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Закупка №{id} удалена.";
        return RedirectToAction(nameof(Index), new { searchTerm, supplierId, status });
    }

    private async Task<PurchaseOrdersIndexViewModel> BuildIndexViewModelAsync(
        string? searchTerm,
        int? supplierId,
        string? status,
        PurchaseOrderEditorViewModel? editor = null)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var normalizedStatus = NormalizeStatus(status);

        ViewData["Title"] = "Закупки";
        ViewData["Subtitle"] = "Управление заказами поставщикам, составом закупки по ингредиентам и статусами поступления.";

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .Select(s => new LookupItemViewModel
            {
                Id = s.SupplierId,
                Name = s.CompanyName
            })
            .ToListAsync();

        var ingredients = await _context.Ingredients
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new LookupItemViewModel
            {
                Id = i.IngredientId,
                Name = i.Name + " (" + i.Unit.UnitName + ")"
            })
            .ToListAsync();

        var allOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderDetails)
            .ThenInclude(d => d.Ingredient)
            .Include(p => p.PurchaseOrderDetails)
            .ThenInclude(d => d.Product)
            .OrderByDescending(p => p.OrderDate)
            .ThenByDescending(p => p.Poid)
            .ToListAsync();

        IEnumerable<PurchaseOrder> filteredOrders = allOrders;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            filteredOrders = filteredOrders.Where(p =>
                p.Supplier.CompanyName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                (p.Status?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.Poid.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                p.PurchaseOrderDetails.Any(d => GetDetailDisplayName(d).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)));
        }

        if (supplierId.HasValue)
        {
            filteredOrders = filteredOrders.Where(p => p.SupplierId == supplierId.Value);
        }

        if (normalizedStatus != "all")
        {
            filteredOrders = filteredOrders.Where(p => string.Equals(p.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase));
        }

        return new PurchaseOrdersIndexViewModel
        {
            TotalOrders = allOrders.Count,
            OrdersInProgress = allOrders.Count(IsInProgress),
            OrdersReceived = allOrders.Count(IsReceived),
            SuppliersCount = suppliers.Count,
            SearchTerm = normalizedSearch,
            SupplierId = supplierId,
            Status = normalizedStatus,
            Suppliers = suppliers,
            Ingredients = ingredients,
            StatusOptions = StatusOptions.ToList(),
            PurchaseOrders = filteredOrders
                .Select(p =>
                {
                    var canEdit = p.PurchaseOrderDetails.All(d => d.IngredientId.HasValue);

                    return new PurchaseOrderListItemViewModel
                    {
                        Number = p.Poid,
                        SupplierId = p.SupplierId,
                        SupplierName = p.Supplier.CompanyName,
                        Status = p.Status ?? "Без статуса",
                        OrderDate = p.OrderDate,
                        ExpectedArrivalDate = p.ExpectedArrivalDate,
                        ItemsCount = p.PurchaseOrderDetails.Count,
                        TotalAmount = p.PurchaseOrderDetails.Sum(d => d.OrderedQty * d.UnitCost),
                        CanEdit = canEdit,
                        EditDisabledReason = canEdit
                            ? null
                            : "Закупка содержит старые товарные позиции. Пересоздайте ее уже на ингредиентах.",
                        Details = p.PurchaseOrderDetails
                            .OrderBy(GetDetailDisplayName)
                            .Select(d => new PurchaseOrderDetailListItemViewModel
                            {
                                PodetailId = d.PodetailId,
                                IngredientId = d.IngredientId,
                                IngredientName = GetDetailDisplayName(d),
                                IsLegacyProduct = d.IngredientId is null && d.ProductId.HasValue,
                                OrderedQty = d.OrderedQty,
                                ReceivedQty = d.ReceivedQty,
                                UnitCost = d.UnitCost,
                                LineTotal = d.OrderedQty * d.UnitCost
                            })
                            .ToList()
                    };
                })
                .ToList(),
            Editor = editor ?? new PurchaseOrderEditorViewModel
            {
                OrderDate = DateTime.Today,
                Status = "Черновик"
            }
        };
    }

    private async Task ValidateEditorAsync(PurchaseOrderEditorViewModel input)
    {
        if (input.ExpectedArrivalDate.HasValue &&
            input.OrderDate.HasValue &&
            input.ExpectedArrivalDate.Value.Date < input.OrderDate.Value.Date)
        {
            ModelState.AddModelError(nameof(PurchaseOrderEditorViewModel.ExpectedArrivalDate), "Ожидаемая дата поставки не может быть раньше даты заказа.");
        }

        var supplierExists = await _context.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.SupplierId == input.SupplierId);

        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(PurchaseOrderEditorViewModel.SupplierId), "Выбранный поставщик не найден.");
        }

        if (input.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(PurchaseOrderEditorViewModel.Details), "Добавьте хотя бы один ингредиент в закупку.");
            return;
        }

        var duplicateIngredients = input.Details
            .GroupBy(d => d.IngredientId)
            .Where(g => g.Key > 0 && g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        if (duplicateIngredients.Count > 0)
        {
            ModelState.AddModelError(nameof(PurchaseOrderEditorViewModel.Details), "Один и тот же ингредиент не должен повторяться в составе закупки.");
        }

        var ingredientIds = input.Details.Select(d => d.IngredientId).Distinct().ToList();
        var existingIngredientIds = await _context.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.IngredientId))
            .Select(i => i.IngredientId)
            .ToListAsync();

        for (var index = 0; index < input.Details.Count; index++)
        {
            var detail = input.Details[index];

            if (!existingIngredientIds.Contains(detail.IngredientId))
            {
                ModelState.AddModelError($"Details[{index}].IngredientId", "Выбранный ингредиент не найден.");
            }

            if (detail.ReceivedQty.HasValue && detail.ReceivedQty.Value > detail.OrderedQty)
            {
                ModelState.AddModelError($"Details[{index}].ReceivedQty", "Принятое количество не может быть больше заказанного.");
            }
        }
    }

    private static void NormalizeEditor(PurchaseOrderEditorViewModel input)
    {
        input.Status = string.IsNullOrWhiteSpace(input.Status) ? "Черновик" : input.Status.Trim();
        input.Details = (input.Details ?? [])
            .Where(d =>
                d.IngredientId > 0 ||
                d.OrderedQty > 0 ||
                (d.ReceivedQty ?? 0) > 0 ||
                d.UnitCost > 0)
            .ToList();

        if (input.Details.Count == 0)
        {
            input.Details =
            [
                new PurchaseOrderDetailEditorViewModel()
            ];
        }
    }

    private static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase)
            ? "all"
            : status.Trim();

    private static bool IsReceived(PurchaseOrder order)
    {
        var value = order.Status?.Trim();
        return string.Equals(value, "Получен", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Received", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Completed", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Завершен", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInProgress(PurchaseOrder order)
    {
        var value = order.Status?.Trim();
        return !string.IsNullOrWhiteSpace(value) &&
               !IsReceived(order) &&
               !string.Equals(value, "Отменен", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(value, "Cancelled", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDetailDisplayName(PurchaseOrderDetail detail)
    {
        if (!string.IsNullOrWhiteSpace(detail.Ingredient?.Name))
        {
            return detail.Ingredient.Name;
        }

        if (!string.IsNullOrWhiteSpace(detail.Product?.NameProduct))
        {
            return $"{detail.Product.NameProduct} (legacy-товар)";
        }

        return "Позиция без ингредиента";
    }
}
