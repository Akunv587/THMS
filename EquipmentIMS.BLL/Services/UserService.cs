using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Exceptions;

namespace EquipmentIMS.BLL.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly PasswordHasher<SysUser> _passwordHasher = new();

    public UserService(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<PagedResult<UserListDto>> GetListAsync(UserQueryDto query)
    {
        var q = _db.SysUsers.Where(u => !u.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(query.Keyword))
            q = q.Where(u => u.UserName.Contains(query.Keyword) || u.RealName.Contains(query.Keyword));
        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        var total = await q.CountAsync();
        var users = await q.OrderByDescending(u => u.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName,
                RealName = u.RealName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive,
                LastLoginTime = u.LastLoginTime,
                CreateTime = u.CreateTime
            })
            .ToListAsync();

        // 批量获取角色
        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await (from ur in _db.SysUserRoles
                               join r in _db.SysRoles on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId) && !r.IsDeleted
                               select new { ur.UserId, r.RoleName }).ToListAsync();

        var roleDict = userRoles.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());
        foreach (var user in users)
            user.Roles = roleDict.GetValueOrDefault(user.Id, new List<string>());

        return new PagedResult<UserListDto>
        {
            Items = users,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UserCreateEditDto?> GetByIdAsync(long id)
    {
        return await _db.SysUsers
            .Where(u => u.Id == id && !u.IsDeleted)
            .Select(u => new UserCreateEditDto
            {
                Id = u.Id,
                UserName = u.UserName,
                RealName = u.RealName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<long> CreateAsync(UserCreateEditDto dto)
    {
        if (await _db.SysUsers.AnyAsync(u => u.UserName == dto.UserName && !u.IsDeleted))
            throw new BusinessException($"用户名 [{dto.UserName}] 已存在", "USERNAME_EXISTS");

        var user = new SysUser
        {
            UserName = dto.UserName,
            RealName = dto.RealName,
            Email = dto.Email,
            Phone = dto.Phone,
            IsActive = dto.IsActive,
            MustChangePwd = true,
            CreateTime = DateTime.Now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "123456");
        user.PasswordSalt = string.Empty;

        _db.SysUsers.Add(user);
        await _db.SaveChangesAsync();
        return user.Id;
    }

    public async Task UpdateAsync(UserCreateEditDto dto)
    {
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted)
            ?? throw new NotFoundException("用户不存在");

        if (await _db.SysUsers.AnyAsync(u => u.UserName == dto.UserName && u.Id != dto.Id && !u.IsDeleted))
            throw new BusinessException($"用户名 [{dto.UserName}] 已存在", "USERNAME_EXISTS");

        user.UserName = dto.UserName;
        user.RealName = dto.RealName;
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;
        user.UpdateTime = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new NotFoundException("用户不存在");

        user.IsDeleted = true;
        user.UpdateTime = DateTime.Now;

        var userRoles = await _db.SysUserRoles.Where(ur => ur.UserId == id).ToListAsync();
        _db.SysUserRoles.RemoveRange(userRoles);

        await _db.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(long id, string newPassword)
    {
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new NotFoundException("用户不存在");

        _db.SysPasswordHistories.Add(new SysPasswordHistory
        {
            UserId = id,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            CreateTime = DateTime.Now
        });

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.PasswordSalt = string.Empty;
        user.MustChangePwd = true;
        user.LastPasswordChanged = DateTime.Now;
        user.UpdateTime = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(long id)
    {
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new NotFoundException("用户不存在");

        user.IsActive = !user.IsActive;
        user.UpdateTime = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task<AssignRolesDto> GetAssignRolesAsync(long id)
    {
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new NotFoundException("用户不存在");

        var allRoles = await _db.SysRoles.Where(r => !r.IsDeleted)
            .Select(r => new RoleOptionDto { Id = r.Id, RoleCode = r.RoleCode, RoleName = r.RoleName })
            .ToListAsync();

        var selectedIds = await _db.SysUserRoles.Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId).ToListAsync();

        var selectedSet = new HashSet<long>(selectedIds);
        foreach (var r in allRoles) r.IsSelected = selectedSet.Contains(r.Id);

        return new AssignRolesDto
        {
            UserId = id,
            UserName = user.UserName,
            RealName = user.RealName,
            AllRoles = allRoles,
            SelectedRoleIds = selectedIds
        };
    }

    public async Task AssignRolesAsync(long userId, List<long> roleIds)
    {
        var existing = await _db.SysUserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _db.SysUserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds.Distinct())
            _db.SysUserRoles.Add(new SysUserRole { UserId = userId, RoleId = roleId });

        await _db.SaveChangesAsync();
    }

    public async Task<List<RoleOptionDto>> GetAllRolesAsync()
    {
        return await _db.SysRoles.Where(r => !r.IsDeleted)
            .Select(r => new RoleOptionDto { Id = r.Id, RoleCode = r.RoleCode, RoleName = r.RoleName })
            .ToListAsync();
    }

    // ==================== 密码历史 (C.3) ====================

    public async Task<List<SysPasswordHistory>> GetPasswordHistoriesAsync(long userId, int count)
    {
        return await _db.SysPasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreateTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task AddPasswordHistoryAsync(SysPasswordHistory history)
    {
        history.CreateTime = DateTime.Now;
        _db.SysPasswordHistories.Add(history);
        await _db.SaveChangesAsync();
    }

    public async Task TrimPasswordHistoryAsync(long userId, int keepCount)
    {
        var oldRecords = await _db.SysPasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreateTime)
            .Skip(keepCount)
            .ToListAsync();

        if (oldRecords.Count > 0)
        {
            _db.SysPasswordHistories.RemoveRange(oldRecords);
            await _db.SaveChangesAsync();
        }
    }
}
