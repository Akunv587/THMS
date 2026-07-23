using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_OperationLog")]
public class SysOperationLog
{
    public long Id { get; set; }
    public long UserId { get; set; }

    [Required, MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Module { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TargetType { get; set; }

    public long? TargetId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    [MaxLength(50)]
    public string? IP { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.Now;
}
