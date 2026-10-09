using AdminDashboard.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace AdminDashboard.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment env) { _logger = logger; _env = env; }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception in {Action}", context.ActionDescriptor.DisplayName);

        if (!context.HttpContext.Request.Path.StartsWithSegments("/api")) return; // الصفحات → UseExceptionHandler

        var msg = _env.IsDevelopment() ? context.Exception.Message : "حدث خطأ غير متوقع";
        context.Result = new ObjectResult(ApiResponse<object>.Fail(msg)) { StatusCode = 500 };
        context.ExceptionHandled = true;
    }
}
