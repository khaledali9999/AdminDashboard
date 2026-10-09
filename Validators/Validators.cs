using AdminDashboard.Data;
using AdminDashboard.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace AdminDashboard.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator(AppDbContext db)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("السعر لازم يكون أكبر من صفر");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).MustAsync((id, ct) => db.Categories.AnyAsync(c => c.Id == id, ct)).WithMessage("التصنيف غير موجود");
    }
}

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator(AppDbContext db)
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("الطلب فاضي");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.Quantity).InclusiveBetween(1, 1000).WithMessage("الكمية بين 1 و 1000");
            i.RuleFor(x => x.ProductId).MustAsync((id, ct) => db.Products.AnyAsync(p => p.Id == id, ct)).WithMessage("منتج غير موجود");
        });
    }
}
