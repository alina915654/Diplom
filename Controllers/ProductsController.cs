using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers;

[Authorize(Roles = "Администратор,Менеджер")]
public class ProductsController : Controller
{
    private readonly DiplomDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(DiplomDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int? categoryId, string activityState = "all")
    {
        return View(await BuildIndexViewModelAsync(searchTerm, categoryId, activityState));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(
        ProductEditorViewModel input,
        string? searchTerm,
        int? categoryId,
        string activityState = "all")
    {
        NormalizeProductInput(input);
        await ValidateProductAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, categoryId, activityState, productEditor: input));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var uploadedPhotoPath = await SaveProductImageAsync(input.Photo);

        var product = new Product
        {
            NameProduct = input.NameProduct,
            CategoryId = input.CategoryId,
            Price = input.Price,
            Description = input.Description,
            PhotoPath = uploadedPhotoPath,
            IsActive = input.IsActive,
            StockQuantity = input.StockQuantity,
            RecipeMethod = input.RecipeMethod
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var inventory = new Inventory
        {
            ProductId = product.ProductId,
            StockLevel = input.StockQuantity,
            MinStockLevel = 5,
            ReorderPoint = 10,
            ExpirationDate = null
        };

        _context.Inventories.Add(inventory);
        _context.WarehouseLogs.Add(new WarehouseLog
        {
            ProductId = product.ProductId,
            ActionType = "Создание",
            Quantity = input.StockQuantity,
            Timestamp = DateTime.Now,
            Note = "Товар создан из модуля управления ассортиментом."
        });

        if (input.StockQuantity > 0)
        {
            _context.Movements.Add(new Movement
            {
                ProductId = product.ProductId,
                MovementType = "Приход",
                Quantity = input.StockQuantity,
                MovementDate = DateTime.Now,
                Reference = "Создание товара"
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Товар «{product.NameProduct}» создан.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(
        ProductEditorViewModel input,
        string? searchTerm,
        int? categoryId,
        string activityState = "all")
    {
        NormalizeProductInput(input);

        if (!input.ProductId.HasValue)
        {
            TempData["ErrorMessage"] = "Не удалось определить товар для редактирования.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        await ValidateProductAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, categoryId, activityState, productEditor: input));
        }

        var product = await _context.Products
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.ProductId == input.ProductId.Value);

        if (product is null)
        {
            TempData["ErrorMessage"] = "Товар не найден.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var oldStock = product.StockQuantity;
        var oldPhotoPath = product.PhotoPath;
        var uploadedPhotoPath = await SaveProductImageAsync(input.Photo);

        product.NameProduct = input.NameProduct;
        product.CategoryId = input.CategoryId;
        product.Price = input.Price;
        product.Description = input.Description;
        product.PhotoPath = uploadedPhotoPath ?? input.ExistingPhotoPath ?? oldPhotoPath;
        product.IsActive = input.IsActive;
        product.StockQuantity = input.StockQuantity;
        product.RecipeMethod = input.RecipeMethod;

        var inventory = product.Inventories.FirstOrDefault();
        if (inventory is null)
        {
            inventory = new Inventory
            {
                ProductId = product.ProductId,
                StockLevel = input.StockQuantity,
                MinStockLevel = 5,
                ReorderPoint = 10
            };

            _context.Inventories.Add(inventory);
        }
        else
        {
            inventory.StockLevel = input.StockQuantity;
        }

        if (oldStock != input.StockQuantity)
        {
            _context.Movements.Add(new Movement
            {
                ProductId = product.ProductId,
                MovementType = input.StockQuantity > oldStock ? "Приход" : "Списание",
                Quantity = Math.Abs(input.StockQuantity - oldStock),
                MovementDate = DateTime.Now,
                Reference = "Изменение товара"
            });
        }

        _context.WarehouseLogs.Add(new WarehouseLog
        {
            ProductId = product.ProductId,
            ActionType = "Правка",
            Quantity = input.StockQuantity,
            Timestamp = DateTime.Now,
            Note = "Карточка товара обновлена."
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        if (!string.IsNullOrWhiteSpace(uploadedPhotoPath) &&
            !string.Equals(oldPhotoPath, uploadedPhotoPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteManagedProductImage(oldPhotoPath);
        }

        TempData["SuccessMessage"] = $"Товар «{product.NameProduct}» обновлен.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id, string? searchTerm, int? categoryId, string activityState = "all")
    {
        var product = await _context.Products
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            TempData["ErrorMessage"] = "Товар не найден.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        var hasDependencies =
            await _context.SalesDetails.AsNoTracking().AnyAsync(x => x.ProductId == id) ||
            await _context.PurchaseOrderDetails.AsNoTracking().AnyAsync(x => x.ProductId == id) ||
            await _context.Movements.AsNoTracking().AnyAsync(x => x.ProductId == id) ||
            await _context.WarehouseLogs.AsNoTracking().AnyAsync(x => x.ProductId == id) ||
            await _context.WasteManagements.AsNoTracking().AnyAsync(x => x.ProductId == id) ||
            await _context.Recipes.AsNoTracking().AnyAsync(x => x.ProductId == id) ||
            await _context.ProductionCalendars.AsNoTracking().AnyAsync(x => x.ProductId == id);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (hasDependencies)
        {
            var oldStock = product.StockQuantity;
            product.IsActive = false;
            product.StockQuantity = 0;

            var inventory = product.Inventories.FirstOrDefault();
            if (inventory is not null)
            {
                inventory.StockLevel = 0;
            }

            _context.WarehouseLogs.Add(new WarehouseLog
            {
                ProductId = product.ProductId,
                ActionType = "Правка",
                Quantity = 0,
                Timestamp = DateTime.Now,
                Note = "Товар деактивирован вместо удаления из-за связанных данных."
            });

            if (oldStock > 0)
            {
                _context.Movements.Add(new Movement
                {
                    ProductId = product.ProductId,
                    MovementType = "Списание",
                    Quantity = oldStock,
                    MovementDate = DateTime.Now,
                    Reference = "Деактивация товара"
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["WarningMessage"] = $"Товар «{product.NameProduct}» не удален физически из-за связанных данных. Он переведен в неактивный статус.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        if (product.Inventories.Count > 0)
        {
            _context.Inventories.RemoveRange(product.Inventories);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        DeleteManagedProductImage(product.PhotoPath);

        TempData["SuccessMessage"] = $"Товар «{product.NameProduct}» удален.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        CategoryEditorViewModel input,
        string? searchTerm,
        int? categoryId,
        string activityState = "all")
    {
        NormalizeCategoryInput(input);
        await ValidateCategoryAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, categoryId, activityState, categoryEditor: input));
        }

        var category = new Category
        {
            NameCategory = input.NameCategory
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Категория «{category.NameCategory}» создана.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(
        CategoryEditorViewModel input,
        string? searchTerm,
        int? categoryId,
        string activityState = "all")
    {
        NormalizeCategoryInput(input);

        if (!input.CategoryId.HasValue)
        {
            TempData["ErrorMessage"] = "Не удалось определить категорию для редактирования.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        await ValidateCategoryAsync(input);

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexViewModelAsync(searchTerm, categoryId, activityState, categoryEditor: input));
        }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == input.CategoryId.Value);
        if (category is null)
        {
            TempData["ErrorMessage"] = "Категория не найдена.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        category.NameCategory = input.NameCategory;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Категория «{category.NameCategory}» обновлена.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, string? searchTerm, int? categoryId, string activityState = "all")
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            TempData["ErrorMessage"] = "Категория не найдена.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        if (category.Products.Count > 0)
        {
            TempData["WarningMessage"] = $"Категорию «{category.NameCategory}» нельзя удалить, пока в ней есть товары.";
            return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Категория «{category.NameCategory}» удалена.";
        return RedirectToAction(nameof(Index), new { searchTerm, categoryId, activityState });
    }

    private async Task<ProductsIndexViewModel> BuildIndexViewModelAsync(
        string? searchTerm,
        int? categoryId,
        string? activityState,
        ProductEditorViewModel? productEditor = null,
        CategoryEditorViewModel? categoryEditor = null)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var normalizedState = NormalizeActivityState(activityState);

        ViewData["Title"] = "Товары и категории";
        ViewData["Subtitle"] = "Управление ассортиментом, активностью товаров, категориями и базовыми складскими остатками.";

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.NameCategory)
            .Select(c => new LookupItemViewModel
            {
                Id = c.CategoryId,
                Name = c.NameCategory
            })
            .ToListAsync();

        var allProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.NameProduct)
            .ToListAsync();

        IEnumerable<Product> filteredProducts = allProducts;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            filteredProducts = filteredProducts.Where(p =>
                p.NameProduct.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                p.Category.NameCategory.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (categoryId.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => p.CategoryId == categoryId.Value);
        }

        filteredProducts = normalizedState switch
        {
            "active" => filteredProducts.Where(p => p.IsActive),
            "inactive" => filteredProducts.Where(p => !p.IsActive),
            "lowstock" => filteredProducts.Where(p => p.StockQuantity <= 5),
            _ => filteredProducts
        };

        var categoryItems = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.NameCategory)
            .ToListAsync();

        return new ProductsIndexViewModel
        {
            TotalProducts = allProducts.Count,
            ActiveProducts = allProducts.Count(p => p.IsActive),
            TotalCategories = categories.Count,
            LowStockProducts = allProducts.Count(p => p.StockQuantity <= 5),
            SearchTerm = normalizedSearch,
            CategoryId = categoryId,
            ActivityState = normalizedState,
            Categories = categories,
            Products = filteredProducts
                .Select(p => new ProductListItemViewModel
                {
                    ProductId = p.ProductId,
                    NameProduct = p.NameProduct,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.NameCategory,
                    Price = p.Price,
                    Description = p.Description ?? string.Empty,
                    PhotoPath = p.PhotoPath ?? string.Empty,
                    IsActive = p.IsActive,
                    StockQuantity = p.StockQuantity,
                    RecipeMethod = p.RecipeMethod ?? string.Empty
                })
                .ToList(),
            CategoryItems = categoryItems
                .Select(c => new CategoryListItemViewModel
                {
                    CategoryId = c.CategoryId,
                    NameCategory = c.NameCategory,
                    ProductCount = c.Products.Count,
                    ActiveProductCount = c.Products.Count(p => p.IsActive)
                })
                .ToList(),
            ProductEditor = productEditor ?? new ProductEditorViewModel
            {
                IsActive = true
            },
            CategoryEditor = categoryEditor ?? new CategoryEditorViewModel()
        };
    }

    private async Task ValidateProductAsync(ProductEditorViewModel input)
    {
        var categoryExists = await _context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.CategoryId == input.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(ProductEditorViewModel.CategoryId), "Выбранная категория не найдена.");
        }

        var duplicateNameExists = await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.NameProduct == input.NameProduct && p.ProductId != input.ProductId);

        if (duplicateNameExists)
        {
            ModelState.AddModelError(nameof(ProductEditorViewModel.NameProduct), "Товар с таким названием уже существует.");
        }

        if (input.Photo is not null)
        {
            var extension = Path.GetExtension(input.Photo.FileName);
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".gif"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(ProductEditorViewModel.Photo), "Допустимы только изображения JPG, PNG, WEBP или GIF.");
            }

            if (input.Photo.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(ProductEditorViewModel.Photo), "Размер изображения не должен превышать 5 МБ.");
            }
        }
    }

    private async Task ValidateCategoryAsync(CategoryEditorViewModel input)
    {
        var duplicateNameExists = await _context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.NameCategory == input.NameCategory && c.CategoryId != input.CategoryId);

        if (duplicateNameExists)
        {
            ModelState.AddModelError(nameof(CategoryEditorViewModel.NameCategory), "Категория с таким названием уже существует.");
        }
    }

    private static void NormalizeProductInput(ProductEditorViewModel input)
    {
        input.NameProduct = input.NameProduct?.Trim() ?? string.Empty;
        input.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        input.ExistingPhotoPath = string.IsNullOrWhiteSpace(input.ExistingPhotoPath) ? null : input.ExistingPhotoPath.Trim();
        input.RecipeMethod = string.IsNullOrWhiteSpace(input.RecipeMethod) ? null : input.RecipeMethod.Trim();
    }

    private static void NormalizeCategoryInput(CategoryEditorViewModel input)
    {
        input.NameCategory = input.NameCategory?.Trim() ?? string.Empty;
    }

    private static string NormalizeActivityState(string? activityState) =>
        activityState?.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "inactive" => "inactive",
            "lowstock" => "lowstock",
            _ => "all"
        };

    private async Task<string?> SaveProductImageAsync(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(photo.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await photo.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private void DeleteManagedProductImage(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath) ||
            !photoPath.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativePath = photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
