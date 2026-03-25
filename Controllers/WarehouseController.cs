using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers;

[Authorize(Roles = "Администратор,Менеджер")]
public class WarehouseController : Controller
{
    private readonly DiplomDbContext _context;

    public WarehouseController(DiplomDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int? categoryId, string stockState = "all")
    {
        return View(await BuildIndexViewModelAsync(searchTerm, categoryId, stockState));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInventory(
        WarehouseInventoryEditorViewModel input,
        string? searchTerm,
        int? categoryId,
        string stockState = "all")
    {
        await ValidateInventoryEditorAsync(input, isEdit: false, originalInventoryId: null);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, categoryId, stockState, input));
        }

        var product = await _context.Products.FirstAsync(p => p.ProductId == input.ProductId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var inventory = new Inventory
        {
            ProductId = input.ProductId,
            StockLevel = input.StockLevel,
            MinStockLevel = input.MinStockLevel,
            ReorderPoint = input.ReorderPoint,
            ExpirationDate = input.ExpirationDate
        };

        _context.Inventories.Add(inventory);
        product.StockQuantity = input.StockLevel;

        _context.WarehouseLogs.Add(new WarehouseLog
        {
            ProductId = input.ProductId,
            ActionType = "Создание",
            Quantity = input.StockLevel,
            Timestamp = DateTime.Now,
            Note = "Создана новая карточка складского остатка."
        });

        if (input.StockLevel > 0)
        {
            _context.Movements.Add(new Movement
            {
                ProductId = input.ProductId,
                MovementType = "Приход",
                Quantity = input.StockLevel,
                MovementDate = DateTime.Now,
                Reference = "Создание карточки остатка"
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Остаток по товару «{product.NameProduct}» создан.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, stockState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInventory(
        WarehouseInventoryEditorViewModel input,
        string? searchTerm,
        int? categoryId,
        string stockState = "all")
    {
        if (!input.InventoryId.HasValue)
        {
            TempData["ErrorMessage"] = "Не удалось определить карточку остатка для редактирования.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, stockState });
        }

        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.InventoryId == input.InventoryId.Value);

        if (inventory is null)
        {
            TempData["ErrorMessage"] = "Карточка складского остатка не найдена.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, stockState });
        }

        await ValidateInventoryEditorAsync(input, isEdit: true, originalInventoryId: inventory.InventoryId);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, categoryId, stockState, input));
        }

        var oldProductId = inventory.ProductId;
        var oldStockLevel = inventory.StockLevel;

        var newProduct = await _context.Products.FirstAsync(p => p.ProductId == input.ProductId);
        Product? oldProduct = oldProductId == input.ProductId
            ? newProduct
            : await _context.Products.FirstAsync(p => p.ProductId == oldProductId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        inventory.ProductId = input.ProductId;
        inventory.StockLevel = input.StockLevel;
        inventory.MinStockLevel = input.MinStockLevel;
        inventory.ReorderPoint = input.ReorderPoint;
        inventory.ExpirationDate = input.ExpirationDate;

        if (oldProductId != input.ProductId)
        {
            oldProduct.StockQuantity = 0;
            newProduct.StockQuantity = input.StockLevel;

            if (oldStockLevel > 0)
            {
                _context.Movements.Add(new Movement
                {
                    ProductId = oldProductId,
                    MovementType = "Списание",
                    Quantity = oldStockLevel,
                    MovementDate = DateTime.Now,
                    Reference = "Переназначение карточки остатка"
                });
            }

            if (input.StockLevel > 0)
            {
                _context.Movements.Add(new Movement
                {
                    ProductId = input.ProductId,
                    MovementType = "Приход",
                    Quantity = input.StockLevel,
                    MovementDate = DateTime.Now,
                    Reference = "Переназначение карточки остатка"
                });
            }

            _context.WarehouseLogs.Add(new WarehouseLog
            {
                ProductId = oldProductId,
                ActionType = "Правка",
                Quantity = oldStockLevel,
                Timestamp = DateTime.Now,
                Note = "Карточка остатка была переназначена на другой товар."
            });
        }
        else
        {
            newProduct.StockQuantity = input.StockLevel;

            var delta = input.StockLevel - oldStockLevel;
            if (delta != 0)
            {
                _context.Movements.Add(new Movement
                {
                    ProductId = input.ProductId,
                    MovementType = delta > 0 ? "Приход" : "Списание",
                    Quantity = Math.Abs(delta),
                    MovementDate = DateTime.Now,
                    Reference = "Корректировка карточки остатка"
                });
            }
        }

        _context.WarehouseLogs.Add(new WarehouseLog
        {
            ProductId = input.ProductId,
            ActionType = "Правка",
            Quantity = input.StockLevel,
            Timestamp = DateTime.Now,
            Note = oldProductId == input.ProductId
                ? "Параметры складского остатка обновлены."
                : "Карточка остатка переназначена на текущий товар."
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Карточка остатка по товару «{newProduct.NameProduct}» обновлена.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, stockState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteInventory(int id, string? searchTerm, int? categoryId, string stockState = "all")
    {
        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.InventoryId == id);

        if (inventory is null)
        {
            TempData["ErrorMessage"] = "Карточка складского остатка не найдена.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, stockState });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        inventory.Product.StockQuantity = 0;

        _context.WarehouseLogs.Add(new WarehouseLog
        {
            ProductId = inventory.ProductId,
            ActionType = "Удаление",
            Quantity = inventory.StockLevel,
            Timestamp = DateTime.Now,
            Note = "Карточка складского остатка удалена."
        });

        if (inventory.StockLevel > 0)
        {
            _context.Movements.Add(new Movement
            {
                ProductId = inventory.ProductId,
                MovementType = "Списание",
                Quantity = inventory.StockLevel,
                MovementDate = DateTime.Now,
                Reference = "Удаление карточки остатка"
            });
        }

        _context.Inventories.Remove(inventory);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Карточка остатка по товару «{inventory.Product.NameProduct}» удалена.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, stockState });
    }

    private async Task<WarehouseIndexViewModel> BuildIndexViewModelAsync(
        string? searchTerm,
        int? categoryId,
        string? stockState,
        WarehouseInventoryEditorViewModel? editor = null)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var normalizedStockState = NormalizeStockState(stockState);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var warningDate = today.AddDays(7);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        ViewData["Title"] = "Склад";
        ViewData["Subtitle"] = "Управление остатками, сроками хранения, закупками и журналом складских операций.";

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.NameCategory)
            .Select(c => new LookupItemViewModel
            {
                Id = c.CategoryId,
                Name = c.NameCategory
            })
            .ToListAsync();

        var products = await _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.NameProduct)
            .Select(p => new LookupItemViewModel
            {
                Id = p.ProductId,
                Name = p.NameProduct
            })
            .ToListAsync();

        var inventoryQuery = _context.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            inventoryQuery = inventoryQuery.Where(i =>
                i.Product.NameProduct.Contains(normalizedSearch) ||
                i.Product.Category.NameCategory.Contains(normalizedSearch));
        }

        if (categoryId.HasValue)
        {
            inventoryQuery = inventoryQuery.Where(i => i.Product.CategoryId == categoryId.Value);
        }

        inventoryQuery = normalizedStockState switch
        {
            "low" => inventoryQuery.Where(i => i.StockLevel <= i.MinStockLevel),
            "out" => inventoryQuery.Where(i => i.StockLevel <= 0),
            "expiring" => inventoryQuery.Where(i => i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= warningDate),
            "ok" => inventoryQuery.Where(i =>
                i.StockLevel > i.MinStockLevel &&
                i.StockLevel > 0 &&
                (i.ExpirationDate == null || i.ExpirationDate > warningDate)),
            _ => inventoryQuery
        };

        var filteredInventory = await inventoryQuery
            .OrderBy(i => i.StockLevel <= i.MinStockLevel ? 0 : 1)
            .ThenBy(i => i.ExpirationDate == null ? 1 : 0)
            .ThenBy(i => i.ExpirationDate)
            .ThenBy(i => i.Product.NameProduct)
            .ToListAsync();

        var allInventory = await _context.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .ToListAsync();

        var expiringItems = allInventory
            .Where(i => i.StockLevel > 0 && i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= warningDate)
            .OrderBy(i => i.ExpirationDate)
            .Take(6)
            .Select(i => new ExpiringInventoryItemViewModel
            {
                ProductName = i.Product.NameProduct,
                ExpirationDate = i.ExpirationDate!.Value,
                StockLevel = i.StockLevel,
                DaysLeft = i.ExpirationDate.Value.DayNumber - today.DayNumber
            })
            .ToList();

        return new WarehouseIndexViewModel
        {
            TotalStockUnits = allInventory.Sum(i => i.StockLevel),
            LowStockProducts = allInventory.Count(i => i.StockLevel <= i.MinStockLevel),
            ExpiringItemsCount = allInventory.Count(i => i.StockLevel > 0 && i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= warningDate),
            WasteThisMonth = await _context.WasteManagements
                .AsNoTracking()
                .Where(w => w.WasteDate >= monthStart && w.WasteDate < nextMonthStart)
                .Select(w => (int?)w.Quantity)
                .SumAsync() ?? 0,
            OpenPurchaseOrders = await _context.PurchaseOrders
                .AsNoTracking()
                .CountAsync(p =>
                    p.Status != null &&
                    p.Status != "Received" &&
                    p.Status != "Получен" &&
                    p.Status != "Completed" &&
                    p.Status != "Завершен"),
            SearchTerm = normalizedSearch,
            CategoryId = categoryId,
            StockState = normalizedStockState,
            Categories = categories,
            Products = products,
            InventoryItems = filteredInventory
                .Select(i => new InventoryListItemViewModel
                {
                    InventoryId = i.InventoryId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.NameProduct,
                    CategoryName = i.Product.Category.NameCategory,
                    StockLevel = i.StockLevel,
                    MinStockLevel = i.MinStockLevel,
                    ReorderPoint = i.ReorderPoint,
                    ExpirationDate = i.ExpirationDate,
                    IsLowStock = i.StockLevel <= i.MinStockLevel,
                    IsExpiringSoon = i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= warningDate
                })
                .ToList(),
            Editor = editor ?? new WarehouseInventoryEditorViewModel
            {
                MinStockLevel = 5,
                ReorderPoint = 10
            },
            LowStockItems = allInventory
                .Where(i => i.StockLevel <= i.MinStockLevel)
                .OrderBy(i => i.StockLevel)
                .ThenBy(i => i.Product.NameProduct)
                .Take(8)
                .Select(i => new StockAlertItemViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.NameProduct,
                    CategoryName = i.Product.Category.NameCategory,
                    StockQuantity = i.StockLevel,
                    Threshold = i.MinStockLevel
                })
                .ToList(),
            ExpiringItems = expiringItems,
            Balances = await _context.ViewWarehouseBalances
                .AsNoTracking()
                .OrderByDescending(b => b.TotalStock ?? 0)
                .Take(8)
                .Select(b => new WarehouseBalanceItemViewModel
                {
                    CategoryName = b.NameCategory,
                    TotalStock = b.TotalStock ?? 0
                })
                .ToListAsync(),
            PurchaseOrders = await _context.PurchaseOrders
                .AsNoTracking()
                .OrderByDescending(p => p.OrderDate)
                .Take(8)
                .Select(p => new PurchaseOrderListItemViewModel
                {
                    Number = p.Poid,
                    SupplierName = p.Supplier.CompanyName,
                    Status = p.Status ?? "Без статуса",
                    OrderDate = p.OrderDate,
                    ExpectedArrivalDate = p.ExpectedArrivalDate,
                    ItemsCount = p.PurchaseOrderDetails.Count
                })
                .ToListAsync(),
            Logs = await _context.WarehouseLogs
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .Select(l => new WarehouseLogListItemViewModel
                {
                    Timestamp = l.Timestamp,
                    ProductName = l.Product != null ? l.Product.NameProduct : "Не указан",
                    ActionType = l.ActionType,
                    Quantity = l.Quantity,
                    Note = l.Note ?? string.Empty
                })
                .ToListAsync()
        };
    }

    private async Task ValidateInventoryEditorAsync(
        WarehouseInventoryEditorViewModel input,
        bool isEdit,
        int? originalInventoryId)
    {
        if (isEdit && !input.InventoryId.HasValue)
        {
            ModelState.AddModelError(nameof(WarehouseInventoryEditorViewModel.InventoryId), "Не указан идентификатор карточки остатка.");
        }

        if (input.ReorderPoint < input.MinStockLevel)
        {
            ModelState.AddModelError(nameof(WarehouseInventoryEditorViewModel.ReorderPoint), "Точка заказа не может быть меньше минимального остатка.");
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == input.ProductId);

        if (product is null)
        {
            ModelState.AddModelError(nameof(WarehouseInventoryEditorViewModel.ProductId), "Выбранный товар не найден.");
            return;
        }

        var duplicateInventoryExists = await _context.Inventories
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == input.ProductId && i.InventoryId != originalInventoryId);

        if (duplicateInventoryExists)
        {
            ModelState.AddModelError(nameof(WarehouseInventoryEditorViewModel.ProductId), "Для выбранного товара уже существует карточка складского остатка.");
        }
    }

    private static string NormalizeStockState(string? stockState) =>
        stockState?.Trim().ToLowerInvariant() switch
        {
            "low" => "low",
            "out" => "out",
            "expiring" => "expiring",
            "ok" => "ok",
            _ => "all"
        };
}
