using System.Security.Claims;
using AdminDashboard.Common;
using AdminDashboard.Filters;
using AdminDashboard.Jobs;
using AdminDashboard.ModelBinders;
using AdminDashboard.Services;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
namespace AdminDashboard.Controllers.Api;

[ApiController]
public abstract class ApiBase : ControllerBase
{
    protected int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected IActionResult Ok<T>(T data, string msg = "") => base.Ok(ApiResponse<T>.Ok(data, msg));
    protected IActionResult Fail(string error) => BadRequest(ApiResponse<object>.Fail(error));
    protected IActionResult Fail(IEnumerable<string> errors) => BadRequest(ApiResponse<object>.Fail(errors));
}

[Route("api/products")]
public class ProductsApiController : ApiBase
{
    private readonly ProductService _svc; private readonly LookupCache _lookup; private readonly IValidator<CreateProductDto> _v;
    public ProductsApiController(ProductService s, LookupCache l, IValidator<CreateProductDto> v) { _svc = s; _lookup = l; _v = v; }

    [HttpGet] public async Task<IActionResult> Get(int page = 1, int size = 10, string? search = null, int? categoryId = null)
        => Ok(await _svc.GetPagedAsync(page, size, search, categoryId));

    [HttpGet("categories")] public async Task<IActionResult> Categories() => Ok(await _lookup.GetCategoriesAsync());

    [HttpPost, RequireRole("Admin", "Sales")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var r = await _v.ValidateAsync(dto);
        if (!r.IsValid) return Fail(r.Errors.Select(e => e.ErrorMessage));
        return Ok(await _svc.CreateAsync(dto), "تم إضافة المنتج");
    }

    [HttpDelete("{id:int}"), RequireRole("Admin")]
    public async Task<IActionResult> Delete(int id) => await _svc.DeleteAsync(id) ? Ok(true, "تم الحذف") : Fail("المنتج غير موجود");
}

[Route("api/users"), RequireRole("Admin")]
public class UsersApiController : ApiBase
{
    private readonly UserService _svc; private readonly IBackgroundJobClient _jobs;
    public UsersApiController(UserService s, IBackgroundJobClient j) { _svc = s; _jobs = j; }

    [HttpGet] public async Task<IActionResult> Get(int page = 1, int size = 10, string? search = null) => Ok(await _svc.GetPagedAsync(page, size, search));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var u = await _svc.RegisterAsync(dto);
        _jobs.Enqueue<EmailJobs>(j => j.SendWelcomeEmailAsync(u.Id)); // Background Job
        return Ok(new UserDto(u.Id, u.Email, u.FullName, u.Role), "تم إنشاء المستخدم");
    }
}

[Route("api/orders"), RequireRole("Admin", "Sales")]
public class OrdersApiController : ApiBase
{
    private readonly OrderService _svc; private readonly CartService _cart; private readonly IValidator<CreateOrderDto> _v;
    public OrdersApiController(OrderService s, CartService c, IValidator<CreateOrderDto> v) { _svc = s; _cart = c; _v = v; }

    [HttpGet] public async Task<IActionResult> Get(int page = 1, int size = 10) => Ok(await _svc.GetPagedAsync(page, size));

    // form: items=id:1,qty:2|id:3,qty:5  ← Custom Model Binder
    [HttpPost]
    public async Task<IActionResult> Create([FromForm(Name = "items")][ModelBinder(typeof(OrderItemsModelBinder))] List<OrderItemDto> items)
    {
        if (!ModelState.IsValid) return Fail(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return await PlaceAsync(items);
    }

    [HttpPost("checkout")] // من الـ Session Cart
    public async Task<IActionResult> Checkout()
    {
        var res = await PlaceAsync(_cart.Items());
        if (res is OkObjectResult) _cart.Clear();
        return res;
    }

    private async Task<IActionResult> PlaceAsync(List<OrderItemDto> items)
    {
        var v = await _v.ValidateAsync(new CreateOrderDto(items));
        if (!v.IsValid) return Fail(v.Errors.Select(e => e.ErrorMessage).Distinct());
        var (ok, err, id) = await _svc.CreateAsync(UserId, items);
        return ok ? Ok(new { orderId = id }, "تم إنشاء الطلب") : Fail(err!);
    }
}

[Route("api/cart"), RequireRole("Admin", "Sales")]
public class CartApiController : ApiBase
{
    private readonly CartService _cart; public CartApiController(CartService c) => _cart = c;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _cart.GetLinesAsync());
    [HttpPost] public async Task<IActionResult> Add([FromBody] AddToCartDto d) { _cart.Add(d.ProductId, Math.Max(1, d.Quantity)); return Ok(await _cart.GetLinesAsync()); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Remove(int id) { _cart.Remove(id); return Ok(await _cart.GetLinesAsync()); }
}

[Route("api/reports"), RequireRole("Admin", "Sales")]
public class ReportsApiController : ApiBase
{
    private readonly ReportService _svc; private readonly IBackgroundJobClient _jobs;
    public ReportsApiController(ReportService s, IBackgroundJobClient j) { _svc = s; _jobs = j; }

    [HttpGet("summary")] public async Task<IActionResult> Summary() => Ok(await _svc.GetSummaryAsync());

    [HttpPost("sales-pdf")] // عملية تقيلة → Background Job
    public IActionResult SalesPdf()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var id = _jobs.Enqueue<ReportJobs>(j => j.GenerateSalesReportAndEmailAsync(email));
        return Ok(new { jobId = id }, "جاري توليد التقرير وهيتبعت على إيميلك");
    }
}
