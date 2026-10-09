using AdminDashboard.Common;
using AdminDashboard.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
namespace AdminDashboard.Services;

// ---------- Cache للبيانات الثابتة ----------
public class LookupCache
{
    private readonly AppDbContext _db; private readonly IMemoryCache _cache;
    public LookupCache(AppDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

    public async Task<List<Category>> GetCategoriesAsync() =>
        (await _cache.GetOrCreateAsync("categories_all", async e =>
        {
            e.SetAbsoluteExpiration(TimeSpan.FromMinutes(30)).SetSlidingExpiration(TimeSpan.FromMinutes(10));
            return await _db.Categories.AsNoTracking().ToListAsync();
        }))!;

    public Task<List<string>> GetCountriesAsync() =>
        Task.FromResult(_cache.GetOrCreate("countries", e =>
        {
            e.SetAbsoluteExpiration(TimeSpan.FromHours(24));
            return new List<string> { "Egypt", "Saudi Arabia", "UAE", "Kuwait", "Qatar", "Jordan" };
        })!);

    public void InvalidateCategories() => _cache.Remove("categories_all");
}

// ---------- Products ----------
public class ProductService
{
    private readonly AppDbContext _db; public ProductService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> GetPagedAsync(int page, int size, string? search, int? categoryId)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var q = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.Contains(search));
        if (categoryId is > 0) q = q.Where(p => p.CategoryId == categoryId);
        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.Id).Skip((page - 1) * size).Take(size)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock, p.Category!.Name)).ToListAsync();
        return new(items, total, page, size);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var p = new Product { Name = dto.Name.Trim(), Price = dto.Price, Stock = dto.Stock, CategoryId = dto.CategoryId };
        _db.Products.Add(p); await _db.SaveChangesAsync();
        var cat = await _db.Categories.FindAsync(p.CategoryId);
        return new(p.Id, p.Name, p.Price, p.Stock, cat!.Name);
    }

    public async Task<bool> DeleteAsync(int id) =>
        await _db.Products.Where(p => p.Id == id).ExecuteDeleteAsync() > 0;
}

// ---------- Users ----------
public class UserService
{
    private readonly AppDbContext _db; public UserService(AppDbContext db) => _db = db;
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int size, string? search)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var q = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderBy(u => u.Id).Skip((page - 1) * size).Take(size)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Role)).ToListAsync();
        return new(items, total, page, size);
    }

    public async Task<User> RegisterAsync(CreateUserDto dto)
    {
        var u = new User { Email = dto.Email.Trim().ToLower(), FullName = dto.FullName, Role = dto.Role };
        u.PasswordHash = _hasher.HashPassword(u, dto.Password);
        _db.Users.Add(u); await _db.SaveChangesAsync();
        return u;
    }

    public async Task<User?> ValidateAsync(string email, string password)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == email.Trim().ToLower());
        return u != null && _hasher.VerifyHashedPassword(u, u.PasswordHash, password) != PasswordVerificationResult.Failed ? u : null;
    }
}

// ---------- Orders ----------
public class OrderService
{
    private readonly AppDbContext _db; public OrderService(AppDbContext db) => _db = db;

    public async Task<PagedResult<OrderDto>> GetPagedAsync(int page, int size)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var total = await _db.Orders.CountAsync();
        var items = await _db.Orders.AsNoTracking().OrderByDescending(o => o.Id).Skip((page - 1) * size).Take(size)
            .Select(o => new OrderDto(o.Id, o.User!.FullName, o.CreatedAt, o.Total, o.Items.Count)).ToListAsync();
        return new(items, total, page, size);
    }

    public async Task<(bool Ok, string? Error, int OrderId)> CreateAsync(int userId, List<OrderItemDto> items)
    {
        var merged = items.GroupBy(i => i.ProductId).Select(g => new OrderItemDto(g.Key, g.Sum(x => x.Quantity))).ToList();
        var ids = merged.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var order = new Order { UserId = userId };
        foreach (var i in merged)
        {
            var p = products[i.ProductId];
            if (p.Stock < i.Quantity) return (false, $"المخزون غير كافي للمنتج {p.Name}", 0);
            p.Stock -= i.Quantity;
            order.Items.Add(new OrderItem { ProductId = p.Id, Quantity = i.Quantity, UnitPrice = p.Price });
        }
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        _db.Orders.Add(order); await _db.SaveChangesAsync();
        return (true, null, order.Id);
    }
}

// ---------- Cart (Session) ----------
public class CartService
{
    private const string Key = "cart"; private readonly IHttpContextAccessor _http; private readonly AppDbContext _db;
    public CartService(IHttpContextAccessor http, AppDbContext db) { _http = http; _db = db; }
    private ISession Session => _http.HttpContext!.Session;

    private Dictionary<int, int> Load() =>
        JsonSerializer.Deserialize<Dictionary<int, int>>(Session.GetString(Key) ?? "{}") ?? new();
    private void Save(Dictionary<int, int> c) => Session.SetString(Key, JsonSerializer.Serialize(c));

    public void Add(int productId, int qty) { var c = Load(); c[productId] = c.GetValueOrDefault(productId) + qty; Save(c); }
    public void Remove(int productId) { var c = Load(); c.Remove(productId); Save(c); }
    public void Clear() => Session.Remove(Key);
    public List<OrderItemDto> Items() => Load().Select(kv => new OrderItemDto(kv.Key, kv.Value)).ToList();

    public async Task<List<CartLine>> GetLinesAsync()
    {
        var c = Load(); var ids = c.Keys.ToList();
        var ps = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();
        return ps.Select(p => new CartLine(p.Id, p.Name, p.Price, c[p.Id])).ToList();
    }
}

// ---------- Reports ----------
public record SummaryDto(int Users, int Products, int Orders, decimal Revenue, List<TopProduct> Top);
public record TopProduct(string Name, int Sold);

public class ReportService
{
    private readonly AppDbContext _db; private readonly IMemoryCache _cache;
    public ReportService(AppDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

    public async Task<SummaryDto> GetSummaryAsync() =>
        (await _cache.GetOrCreateAsync("summary", async e =>
        {
            e.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            var revenue = (await _db.Orders.Select(o => (double?)o.Total).SumAsync()) ?? 0;
            var top = await _db.OrderItems.GroupBy(i => i.Product!.Name)
                .Select(g => new TopProduct(g.Key, g.Sum(x => x.Quantity))).OrderByDescending(t => t.Sold).Take(5).ToListAsync();
            return new SummaryDto(await _db.Users.CountAsync(), await _db.Products.CountAsync(), await _db.Orders.CountAsync(), (decimal)revenue, top);
        }))!;
}

// ---------- Email (Fake: بيكتب في اللوج) ----------
public interface IEmailService { Task SendAsync(string to, string subject, string body, string? attachment = null); }
public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _log; public FakeEmailService(ILogger<FakeEmailService> log) => _log = log;
    public async Task SendAsync(string to, string subject, string body, string? attachment = null)
    {
        await Task.Delay(1500); // محاكاة SMTP بطيء
        _log.LogInformation("📧 Email to {To} | {Subject} | attachment: {File}", to, subject, attachment ?? "-");
    }
}
