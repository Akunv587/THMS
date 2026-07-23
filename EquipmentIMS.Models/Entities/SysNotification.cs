using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_Notification")]
public class SysNotification
{
    public long Id { get; set; }
    public long UserId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Content { get; set; }

    public NotificationType NotificationType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadTime { get; set; }

    [MaxLength(50)]
    public string? RefType { get; set; }

    public long? RefId { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;

    public virtual SysUser User { get; set; } = null!;
}
