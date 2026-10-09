using AdminDashboard.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace AdminDashboard.Filters;

/// Authorization Filter بالـ Role. بيرجّع JSON موحّد للـ API وRedirect للصفحات.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    public RequireRoleAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) return; // الـ AuthorizeFilter العام هو اللي بيتعامل معاه
        if (_roles.Any(user.IsInRole)) return;

        context.Result = context.HttpContext.Request.Path.StartsWithSegments("/api")
            ? new ObjectResult(ApiResponse<object>.Fail("ليس لديك صلاحية")) { StatusCode = 403 }
            : new RedirectResult("/Account/Denied");
    }
}
