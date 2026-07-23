namespace EquipmentIMS.Models.DTOs;

public class NotificationQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsRead { get; set; }
    public int? NotificationType { get; set; }
}
