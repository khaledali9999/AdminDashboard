using AdminDashboard.Common;
using AdminDashboard.Data;
using AdminDashboard.Filters;
using AdminDashboard.Services;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=admin.db"));
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(30); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401; else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();

builder.Services.AddHangfire(c => c.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<LookupCache>().AddScoped<ProductService>().AddScoped<UserService>()
    .AddScoped<OrderService>().AddScoped<CartService>().AddScoped<ReportService>()
    .AddScoped<IEmailService, FakeEmailService>()
    .AddScoped<AdminDashboard.Jobs.EmailJobs>().AddScoped<AdminDashboard.Jobs.ReportJobs>();

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());          // كل الـ Actions محمية
    o.Filters.Add<LogActionFilter>();              // Logging + Timing
    o.Filters.Add<GlobalExceptionFilter>();        // Errors موحّدة
}).ConfigureApiBehaviorOptions(o =>
    o.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(ApiResponse<object>.Fail(
        ctx.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), builder.Configuration.GetValue("Seed:Products", 100_000));

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHangfireDashboard("/jobs", new DashboardOptions { Authorization = new[] { new AdminOnlyDashboardFilter() } });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

class AdminOnlyDashboardFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext c) => c.GetHttpContext().User.IsInRole("Admin");
}
