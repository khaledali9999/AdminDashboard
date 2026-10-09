using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace AdminDashboard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Name);
        b.Entity<Order>().Property(o => o.Total).HasConversion<double>();
        b.Entity<Product>().Property(p => p.Price).HasConversion<double>();
        b.Entity<OrderItem>().Property(p => p.UnitPrice).HasConversion<double>();
    }
}

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db, int productCount)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        var admin = new User { Email = "admin@demo.com", FullName = "Admin", Role = "Admin" };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");
        var sales = new User { Email = "sales@demo.com", FullName = "Sales User", Role = "Sales" };
        sales.PasswordHash = hasher.HashPassword(sales, "Sales@123");
        db.Users.AddRange(admin, sales);
        db.Categories.AddRange(new[] { "Electronics", "Books", "Clothes", "Home", "Toys" }.Select(n => new Category { Name = n }));
        await db.SaveChangesAsync();

        var rnd = new Random(1);
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        for (int i = 0; i < productCount; i += 5000)
        {
            for (int j = i; j < Math.Min(i + 5000, productCount); j++)
                db.Products.Add(new Product { Name = $"Product {j + 1}", Price = rnd.Next(5, 2000), Stock = rnd.Next(0, 500), CategoryId = rnd.Next(1, 6) });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
    }
}
