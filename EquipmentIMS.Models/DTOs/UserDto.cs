namespace EquipmentIMS.Models.DTOs;

public class UserDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreateTime { get; set; }
}
