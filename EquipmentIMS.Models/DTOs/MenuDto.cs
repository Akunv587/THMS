namespace EquipmentIMS.Models.DTOs;

public class MenuDto
{
    public long Id { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public int PermissionType { get; set; }
    public List<MenuDto> Children { get; set; } = new();
}
