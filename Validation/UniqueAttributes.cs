using System.ComponentModel.DataAnnotations;
using AdminDashboard.Data;
namespace AdminDashboard.Validation;

public class UniqueEmailAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        var email = (value as string)?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(email)) return new ValidationResult("الإيميل مطلوب");
        var db = (AppDbContext)ctx.GetService(typeof(AppDbContext))!;
        return db.Users.Any(u => u.Email == email) ? new ValidationResult("هذا الإيميل مستخدم بالفعل") : ValidationResult.Success;
    }
}

public class UniqueProductNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        var name = (value as string)?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return new ValidationResult("اسم المنتج مطلوب");
        var db = (AppDbContext)ctx.GetService(typeof(AppDbContext))!;
        return db.Products.Any(p => p.Name == name) ? new ValidationResult("اسم المنتج موجود بالفعل") : ValidationResult.Success;
    }
}
