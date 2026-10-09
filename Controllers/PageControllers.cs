using System.Security.Claims;
using AdminDashboard.Filters;
using AdminDashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AdminDashboard.Controllers;

public class AccountController : Controller
{
    private readonly UserService _users; public AccountController(UserService u) => _users = u;

    [AllowAnonymous, HttpGet] public IActionResult Login() => View();

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var u = await _users.ValidateAsync(email ?? "", password ?? "");
        if (u == null) { ViewBag.Error = "بيانات الدخول غير صحيحة"; return View(); }
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.FullName),
                             new Claim(ClaimTypes.Email, u.Email), new Claim(ClaimTypes.Role, u.Role) };
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction("Index", "Home");
    }

    [HttpPost] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous] public IActionResult Denied() => Content("403 - ليس لديك صلاحية");
}

public class HomeController : Controller
{
    public IActionResult Index() => View();
    [AllowAnonymous] public IActionResult Error() => Content("حدث خطأ غير متوقع");
}
public class ProductsController : Controller { public IActionResult Index() => View(); }
[RequireRole("Admin")] public class UsersController : Controller { public IActionResult Index() => View(); }
[RequireRole("Admin", "Sales")] public class OrdersController : Controller { public IActionResult Index() => View(); }
[RequireRole("Admin", "Sales")] public class ReportsController : Controller { public IActionResult Index() => View(); }
