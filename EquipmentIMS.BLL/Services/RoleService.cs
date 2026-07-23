using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public RoleService(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<RoleListDto>> GetListAsync()
    {
        return await _db.SysRoles.Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreateTime)
            .Select(r => new RoleListDto
            {
                Id = r.Id,
                RoleCode = r.RoleCode,
                RoleName = r.RoleName,
                Description = r.Description,
                UserCount = r.UserRoles.Count,
                PermissionCount = r.RolePermissions.Count,
                CreateTime = r.CreateTime
            })
            .ToListAsync();
    }

    public async Task<RoleCreateEditDto?> GetByIdAsync(long id)
    {
        return await _db.SysRoles
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(r => new RoleCreateEditDto
            {
                Id = r.Id,
                RoleCode = r.RoleCode,
                RoleName = r.RoleName,
                Description = r.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<long> CreateAsync(RoleCreateEditDto dto)
    {
        if (await _db.SysRoles.AnyAsync(r => r.RoleCode == dto.RoleCode && !r.IsDeleted))
            throw new InvalidOperationException($"角色编码 [{dto.RoleCode}] 已存在");

        var role = new SysRole
        {
            RoleCode = dto.RoleCode,
            RoleName = dto.RoleName,
            Description = dto.Description,
            CreateTime = DateTime.Now
        };
        _db.SysRoles.Add(role);
        await _db.SaveChangesAsync();
        return role.Id;
    }

    public async Task UpdateAsync(RoleCreateEditDto dto)
    {
        var role = await _db.SysRoles.FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted)
            ?? throw new InvalidOperationException("角色不存在");

        if (await _db.SysRoles.AnyAsync(r => r.RoleCode == dto.RoleCode && r.Id != dto.Id && !r.IsDeleted))
            throw new InvalidOperationException($"角色编码 [{dto.RoleCode}] 已存在");

        role.RoleCode = dto.RoleCode;
        role.RoleName = dto.RoleName;
        role.Description = dto.Description;
        role.UpdateTime = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var role = await _db.SysRoles.Include(r => r.UserRoles).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new InvalidOperationException("角色不存在");

        if (role.UserRoles.Count > 0)
            throw new InvalidOperationException("该角色下存在关联用户，无法删除");

        role.IsDeleted = true;
        role.UpdateTime = DateTime.Now;

        var perms = await _db.SysRolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        _db.SysRolePermissions.RemoveRange(perms);

        await _db.SaveChangesAsync();
    }

    public async Task<AssignPermissionsDto> GetAssignPermissionsAsync(long id)
    {
        var role = await _db.SysRoles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new InvalidOperationException("角色不存在");

        var allPerms = await _db.SysPermissions.Where(p => !p.IsDeleted)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync();

        var selectedIds = (await _db.SysRolePermissions.Where(rp => rp.RoleId == id)
            .Select(rp => rp.PermissionId).ToListAsync()).ToHashSet();

        var nodes = allPerms.Select(p => new PermissionTreeDto
        {
            Id = p.Id,
            ParentId = p.ParentId,
            PermissionCode = p.PermissionCode,
            PermissionName = p.PermissionName,
            PermissionType = (int)p.PermissionType,
            PermissionTypeName = p.PermissionType switch
            {
                PermissionType.Menu => "菜单",
                PermissionType.Button => "按钮",
                PermissionType.Data => "数据",
                _ => ""
            },
            SortOrder = p.SortOrder,
            IsChecked = selectedIds.Contains(p.Id)
        }).ToList();

        return new AssignPermissionsDto
        {
            RoleId = id,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            PermissionTree = BuildTree(nodes, null)
        };
    }

    public async Task AssignPermissionsAsync(long roleId, List<long> permissionIds)
    {
        var existing = await _db.SysRolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.SysRolePermissions.RemoveRange(existing);

        foreach (var permId in permissionIds.Distinct())
            _db.SysRolePermissions.Add(new SysRolePermission { RoleId = roleId, PermissionId = permId });

        await _db.SaveChangesAsync();
    }

    private static List<PermissionTreeDto> BuildTree(List<PermissionTreeDto> nodes, long? parentId)
    {
        var children = nodes.Where(n => n.ParentId == parentId).ToList();
        foreach (var child in children)
            child.Children = BuildTree(nodes, child.Id);
        return children;
    }
}
