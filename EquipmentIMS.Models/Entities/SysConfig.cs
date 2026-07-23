using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_Config")]
public class SysConfig : BaseEntity
{
    [Required, MaxLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ConfigValue { get; set; }

    [Required, MaxLength(100)]
    public string ConfigName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ConfigType ConfigType { get; set; } = ConfigType.String;
    public bool IsEditable { get; set; } = true;
}
