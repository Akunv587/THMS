using Microsoft.EntityFrameworkCore;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Repositories;

/// <summary>
/// 操作日志仓储实现
/// </summary>
public class OperationLogRepository : IOperationLogRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<SysOperationLog> _dbSet;

    public OperationLogRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<SysOperationLog>();
    }

    public async Task AddAsync(SysOperationLog log)
    {
        await _dbSet.AddAsync(log);
    }

    public async Task<IEnumerable<SysOperationLog>> QueryAsync(DateTime? startTime, DateTime? endTime,
        string? module, long? userId, int page, int pageSize)
    {
        var query = BuildQuery(startTime, endTime, module, userId);
        return await query
            .OrderByDescending(l => l.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountAsync(DateTime? startTime, DateTime? endTime, string? module, long? userId)
    {
        var query = BuildQuery(startTime, endTime, module, userId);
        return await query.AsNoTracking().CountAsync();
    }

    /// <summary>
    /// 归档指定日期之前的日志（返回归档数量）
    /// </summary>
    public async Task<int> ArchiveAsync(DateTime beforeDate)
    {
        return await _dbSet
            .Where(l => l.CreateTime < beforeDate)
            .ExecuteDeleteAsync();
    }

    private IQueryable<SysOperationLog> BuildQuery(DateTime? startTime, DateTime? endTime,
        string? module, long? userId)
    {
        var query = _dbSet.AsQueryable();
        if (startTime.HasValue) query = query.Where(l => l.CreateTime >= startTime.Value);
        if (endTime.HasValue) query = query.Where(l => l.CreateTime <= endTime.Value);
        if (!string.IsNullOrEmpty(module)) query = query.Where(l => l.Module == module);
        if (userId.HasValue) query = query.Where(l => l.UserId == userId.Value);
        return query;
    }
}
