using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
namespace AdminDashboard.Filters;

/// Logging + Timing لكل Action (مفيش Logging في أي Controller)
public class LogActionFilter : IAsyncActionFilter
{
    private readonly ILogger<LogActionFilter> _logger;
    public LogActionFilter(ILogger<LogActionFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var name = context.ActionDescriptor.DisplayName;
        var user = context.HttpContext.User.Identity?.Name ?? "anonymous";
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("▶ {Action} started by {User}", name, user);

        var executed = await next();

        sw.Stop();
        _logger.LogInformation("✓ {Action} finished in {Ms}ms (exception: {Ex})", name, sw.ElapsedMilliseconds, executed.Exception != null);
        if (sw.ElapsedMilliseconds > 2000)
            _logger.LogWarning("⚠ {Action} took > 2s — consider a Background Job", name);
    }
}
