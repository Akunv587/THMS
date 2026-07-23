namespace EquipmentIMS.Models.DTOs;

public class NotificationDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int NotificationType { get; set; }
    public string? NotificationTypeName { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadTime { get; set; }
    public string? RefType { get; set; }
    public long? RefId { get; set; }
    public DateTime CreateTime { get; set; }
}
