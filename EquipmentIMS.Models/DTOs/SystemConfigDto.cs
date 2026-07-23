namespace EquipmentIMS.Models.DTOs;

public class SystemConfigDto
{
    public long Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ConfigType { get; set; }
    public bool IsEditable { get; set; }
    public DateTime? UpdateTime { get; set; }
}
