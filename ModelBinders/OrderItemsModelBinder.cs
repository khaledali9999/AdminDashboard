using AdminDashboard.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace AdminDashboard.ModelBinders;

/// بيحوّل "id:1,qty:2|id:3,qty:5" → List<OrderItemDto>
public class OrderItemsModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var raw = ctx.ValueProvider.GetValue(ctx.ModelName).FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
        {
            ctx.ModelState.TryAddModelError(ctx.ModelName, "لازم تضيف منتج واحد على الأقل");
            ctx.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }
        try
        {
            var items = raw.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(part =>
            {
                var kv = part.Split(',').Select(p => p.Split(':')).ToDictionary(x => x[0].Trim().ToLower(), x => x[1].Trim());
                return new OrderItemDto(int.Parse(kv["id"]), int.Parse(kv["qty"]));
            }).ToList();
            ctx.Result = ModelBindingResult.Success(items);
        }
        catch
        {
            ctx.ModelState.TryAddModelError(ctx.ModelName, "صيغة المنتجات غير صحيحة. المطلوب: id:1,qty:2|id:3,qty:5");
            ctx.Result = ModelBindingResult.Failed();
        }
        return Task.CompletedTask;
    }
}
