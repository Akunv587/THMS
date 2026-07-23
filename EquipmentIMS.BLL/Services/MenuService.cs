using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    public MenuService(AppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<MenuDto>> GetMenusByUserAsync(long userId)
    {
        var cacheKey = $"Menu:User:{userId}";
        var cached = await _cache.GetAsync<List<MenuDto>>(cacheKey);
        if (cached != null) return cached;

        var roleIds = await _db.SysUserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0)
            return new List<MenuDto>();

        var permissionIds = await _db.SysRolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync();

        if (permissionIds.Count == 0)
            return new List<MenuDto>();

        var permissions = await _db.SysPermissions
            .Where(p => permissionIds.Contains(p.Id)
                && p.PermissionType == PermissionType.Menu
                && !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .Select(p => new MenuDto
            {
                Id = p.Id,
                PermissionCode = p.PermissionCode,
                PermissionName = p.PermissionName,
                Url = p.Url,
                Icon = p.Icon,
                SortOrder = p.SortOrder,
                PermissionType = (int)p.PermissionType
            })
            .ToListAsync();

        var allWithParent = await _db.SysPermissions
            .Where(p => permissionIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync();

        var tree = BuildTree(allWithParent.Where(p => p.PermissionType == PermissionType.Menu).ToList(), null);
        await _cache.SetAsync(cacheKey, tree, CacheExpiration);

        return tree;
    }

    public async Task ClearMenuCacheAsync(long userId)
    {
        await _cache.RemoveAsync($"Menu:User:{userId}");
    }

    private static List<MenuDto> BuildTree(List<Models.Entities.SysPermission> all, long? parentId)
    {
        return all
            .Where(p => p.ParentId == parentId)
            .OrderBy(p => p.SortOrder)
            .Select(p => new MenuDto
            {
                Id = p.Id,
                PermissionCode = p.PermissionCode,
                PermissionName = p.PermissionName,
                Url = p.Url,
                Icon = p.Icon,
                SortOrder = p.SortOrder,
                PermissionType = (int)p.PermissionType,
                Children = BuildTree(all, p.Id)
            })
            .ToList();
    }
}
