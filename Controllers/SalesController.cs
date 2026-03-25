using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Diplom.Controllers
{
    [Authorize(Roles = "Кассир, Администратор")]
    public class SalesController : Controller
    {
        private readonly DiplomDbContext _context;

        public SalesController(DiplomDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.IsActive == true && p.StockQuantity > 0)
                .ToListAsync();

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutViewModel? request)
        {
            if (request is null || request.Items.Count == 0)
            {
                return BadRequest("Корзина пуста!");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Не удалось определить текущего пользователя."
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var totalAmount = request.Items.Sum(i => i.Price * i.Quantity);
                var checkNum = $"CH-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 10000)}";

                var sale = new Sale
                {
                    CheckNumber = checkNum,
                    SaleDate = DateTime.Now,
                    UserId = currentUserId,
                    TotalAmount = totalAmount,
                    FinalAmount = totalAmount,
                    PaymentMethod = request.PaymentMethod,
                    TypeId = request.TypeId,
                    StatusId = request.TypeId == 1 ? 5 : 1
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                foreach (var item in request.Items)
                {
                    var detail = new SalesDetail
                    {
                        SaleId = sale.SaleId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SellingPrice = item.Price,
                        CostPrice = 0m
                    };

                    _context.SalesDetails.Add(detail);
                }

                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_UpdateStockAfterSale {sale.SaleId}");

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    checkNumber = sale.CheckNumber
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
