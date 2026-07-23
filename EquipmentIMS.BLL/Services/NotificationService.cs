using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

/// <summary>
/// 通知服务，直接使用 AppDbContext（SysNotification 不继承 BaseEntity）
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SendAsync(long userId, string title, string content, NotificationType type, string? refType = null, long? refId = null)
    {
        var notification = new SysNotification
        {
            UserId = userId,
            Title = title,
            Content = content,
            NotificationType = type,
            IsRead = false,
            RefType = refType,
            RefId = refId,
            CreateTime = DateTime.Now
        };
        await _context.SysNotifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<NotificationDto>> GetListAsync(long userId, NotificationQueryDto query)
    {
        var q = _context.SysNotifications.AsNoTracking()
            .Where(n => n.UserId == userId);

        if (query.IsRead.HasValue)
            q = q.Where(n => n.IsRead == query.IsRead.Value);
        if (query.NotificationType.HasValue)
            q = q.Where(n => (int)n.NotificationType == query.NotificationType.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(n => n.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Content = n.Content,
                NotificationType = (int)n.NotificationType,
                NotificationTypeName = GetTypeName(n.NotificationType),
                IsRead = n.IsRead,
                ReadTime = n.ReadTime,
                RefType = n.RefType,
                RefId = n.RefId,
                CreateTime = n.CreateTime
            })
            .ToListAsync();

        return new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<NotificationDto?> GetByIdAsync(long notificationId)
    {
        var n = await _context.SysNotifications.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == notificationId);
        if (n == null) return null;

        return new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            NotificationType = (int)n.NotificationType,
            NotificationTypeName = GetTypeName(n.NotificationType),
            IsRead = n.IsRead,
            ReadTime = n.ReadTime,
            RefType = n.RefType,
            RefId = n.RefId,
            CreateTime = n.CreateTime
        };
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _context.SysNotifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<List<NotificationDto>> GetRecentUnreadAsync(long userId, int count = 5)
    {
        return await _context.SysNotifications.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreateTime)
            .Take(count)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Content = n.Content,
                NotificationType = (int)n.NotificationType,
                NotificationTypeName = GetTypeName(n.NotificationType),
                IsRead = n.IsRead,
                ReadTime = n.ReadTime,
                RefType = n.RefType,
                RefId = n.RefId,
                CreateTime = n.CreateTime
            })
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(long notificationId)
    {
        var notification = await _context.SysNotifications.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        var unread = await _context.SysNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadTime = DateTime.Now;
        }

        if (unread.Count > 0)
            await _context.SaveChangesAsync();
    }

    private static string GetTypeName(NotificationType type) => type switch
    {
        NotificationType.ApprovalReminder => "审批提醒",
        NotificationType.StockAlert => "库存预警",
        NotificationType.MaintenanceReminder => "保养提醒",
        NotificationType.SystemNotification => "系统通知",
        _ => ""
    };
}
