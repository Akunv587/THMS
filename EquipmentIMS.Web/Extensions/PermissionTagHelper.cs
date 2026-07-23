using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;

namespace EquipmentIMS.Web.Extensions;

[HtmlTargetElement("permission", TagStructure = TagStructure.NormalOrSelfClosing)]
public class PermissionTagHelper : TagHelper
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;
    private readonly IServiceProvider _serviceProvider;

    public PermissionTagHelper(
        ICurrentUserService currentUser,
        ICacheService cache,
        IServiceProvider serviceProvider)
    {
        _currentUser = currentUser;
        _cache = cache;
        _serviceProvider = serviceProvider;
    }

    [HtmlAttributeName("code")]
    public string Code { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var userId = _currentUser.UserId;
        if (userId <= 0)
        {
            output.SuppressOutput();
            return;
        }

        var cacheKey = $"UserPermission:{userId}";
        var cachedPerms = await _cache.GetAsync<HashSet<string>>(cacheKey);

        if (cachedPerms == null)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DAL.AppDbContext>();

            var roleIds = await db.SysUserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (roleIds.Count == 0)
            {
                output.SuppressOutput();
                return;
            }

            var perms = await db.SysRolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Join(db.SysPermissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.PermissionCode)
                .Distinct()
                .ToListAsync();
            cachedPerms = new HashSet<string>(perms);

            await _cache.SetAsync(cacheKey, cachedPerms, TimeSpan.FromMinutes(10));
        }

        if (!cachedPerms.Contains(Code))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null;
    }
}
