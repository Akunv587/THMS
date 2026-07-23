using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;

namespace EquipmentIMS.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionFilterAttribute : ActionFilterAttribute
{
    private readonly string _permissionCode;
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(10);

    public PermissionFilterAttribute(string permissionCode)
    {
        _permissionCode = permissionCode;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var userId = currentUser.UserId;
        if (userId <= 0)
        {
            SetUnauthenticated(context);
            return;
        }

        // 检查权限缓存
        var cacheKey = $"UserPermission:{userId}";
        var cachedPerms = await cache.GetAsync<HashSet<string>>(cacheKey);

        if (cachedPerms == null)
        {
            var roleIds = await db.SysUserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (roleIds.Count == 0)
            {
                SetForbidden(context);
                return;
            }

            var perms = await db.SysRolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Join(db.SysPermissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.PermissionCode)
                .Distinct()
                .ToListAsync();
            cachedPerms = new HashSet<string>(perms);

            await cache.SetAsync(cacheKey, cachedPerms, CacheTime);
        }

        if (!cachedPerms.Contains(_permissionCode))
        {
            SetForbidden(context);
            return;
        }

        await next();
    }

    private static void SetUnauthenticated(ActionExecutingContext context)
    {
        if (IsAjaxRequest(context.HttpContext.Request))
        {
            context.Result = new JsonResult(new { code = 401, message = "未登录" })
            {
                StatusCode = 401
            };
        }
        else
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }

    private static void SetForbidden(ActionExecutingContext context)
    {
        if (IsAjaxRequest(context.HttpContext.Request))
        {
            context.Result = new JsonResult(new { code = 403, message = "无权限" })
            {
                StatusCode = 403
            };
        }
        else
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest"
            || request.Headers["Accept"].Any(h => h != null && h.Contains("application/json"));
    }
}
