namespace EquipmentIMS.Models.DTOs;

public class RoleDto
{
    public long Id { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<long> PermissionIds { get; set; } = new();
    public DateTime CreateTime { get; set; }
}
