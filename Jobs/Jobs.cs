using AdminDashboard.Data;
using AdminDashboard.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
namespace AdminDashboard.Jobs;

public class EmailJobs
{
    private readonly IEmailService _email; private readonly AppDbContext _db;
    public EmailJobs(IEmailService email, AppDbContext db) { _email = email; _db = db; }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendWelcomeEmailAsync(int userId)
    {
        var u = await _db.Users.FindAsync(userId) ?? throw new InvalidOperationException("User not found");
        await _email.SendAsync(u.Email, "أهلاً بيك!", $"مرحباً {u.FullName}، تم إنشاء حسابك بنجاح.");
    }
}

public class ReportJobs
{
    private readonly AppDbContext _db; private readonly IEmailService _email; private readonly IWebHostEnvironment _env;
    public ReportJobs(AppDbContext db, IEmailService email, IWebHostEnvironment env) { _db = db; _email = email; _env = env; }

    [AutomaticRetry(Attempts = 2)]
    public async Task GenerateSalesReportAndEmailAsync(string toEmail)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var top = await _db.OrderItems.GroupBy(i => i.Product!.Name)
            .Select(g => new { g.Key, Qty = g.Sum(x => x.Quantity) }).OrderByDescending(x => x.Qty).Take(20).ToListAsync();
        var orders = await _db.Orders.CountAsync();
        var revenue = (await _db.Orders.Select(o => (double?)o.Total).SumAsync()) ?? 0;

        var dir = Path.Combine(_env.ContentRootPath, "reports"); Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"sales-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");

        Document.Create(c => c.Page(p =>
        {
            p.Margin(30);
            p.Header().Text("Sales Report").FontSize(22).Bold();
            p.Content().Column(col =>
            {
                col.Item().Text($"Orders: {orders}   |   Revenue: {revenue:N0}");
                col.Item().PaddingTop(10).Text("Top products").Bold();
                foreach (var t in top) col.Item().Text($"{t.Key} — {t.Qty}");
            });
            p.Footer().AlignCenter().Text(x => { x.Span("Page "); x.CurrentPageNumber(); });
        })).GeneratePdf(path);

        await _email.SendAsync(toEmail, "تقرير المبيعات", "مرفق تقرير المبيعات.", path);
    }
}
