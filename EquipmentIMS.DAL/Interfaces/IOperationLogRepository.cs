using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 操作日志仓储接口
/// </summary>
public interface IOperationLogRepository
{
    Task AddAsync(SysOperationLog log);
    Task<IEnumerable<SysOperationLog>> QueryAsync(DateTime? startTime, DateTime? endTime,
        string? module, long? userId, int page, int pageSize);
    Task<int> CountAsync(DateTime? startTime, DateTime? endTime, string? module, long? userId);
    /// <summary>
    /// 归档6个月前的日志
    /// </summary>
    Task<int> ArchiveAsync(DateTime beforeDate);
}
