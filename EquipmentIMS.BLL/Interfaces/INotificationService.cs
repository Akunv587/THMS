using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Interfaces;

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    Task SendAsync(long userId, string title, string content, NotificationType type, string? refType = null, long? refId = null);
    Task<PagedResult<NotificationDto>> GetListAsync(long userId, NotificationQueryDto query);
    Task<NotificationDto?> GetByIdAsync(long notificationId);
    Task<int> GetUnreadCountAsync(long userId);
    Task<List<NotificationDto>> GetRecentUnreadAsync(long userId, int count = 5);
    Task MarkAsReadAsync(long notificationId);
    Task MarkAllAsReadAsync(long userId);
}
