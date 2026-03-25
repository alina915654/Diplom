using Diplom.Models;
using Diplom.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Diplom.Controllers
{
    public class AccountController : Controller
    {
        private readonly DiplomDbContext _context;

        // Внедрение базы данных через конструктор
        public AccountController(DiplomDbContext context)
        {
            _context = context;
        }

        // 1. Отдача HTML-формы для входа (Login.cshtml)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 2. Кнопка входа в системе (принимаем данные из формы, проверяем, авторизуем)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Login == model.Login && u.Password == model.Password);

                if (user != null)
                {
                    // Успешный вход - сбрасываем счетчик ошибок
                    TempData.Remove("FailedAttempts");

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Fio),
                        new Claim(ClaimTypes.Role, user.Role.RoleName)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }

                // Логика неверного пароля: увеличиваем счетчик
                int failedAttempts = TempData.Peek("FailedAttempts") as int? ?? 0;
                failedAttempts++;
                TempData["FailedAttempts"] = failedAttempts;

                // Добавляем ошибку для вывода на экран
                ModelState.AddModelError("AuthError", "Некорректные логин и (или) пароль");
            }
            return View(model);
        }

        // Заглушка для страницы восстановления пароля
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return Content("Здесь будет страница восстановления пароля.");
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Доступ запрещен";
            ViewData["Subtitle"] = "У вас недостаточно прав для открытия этого раздела.";
            return View();
        }

        // 3. Выход из системы
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}