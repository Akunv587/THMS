using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_Permission")]
public class SysPermission : BaseEntity
{
    public long? ParentId { get; set; }

    [Required, MaxLength(100)]
    public string PermissionCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PermissionName { get; set; } = string.Empty;

    public PermissionType PermissionType { get; set; }

    [MaxLength(200)]
    public string? Url { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public virtual SysPermission? Parent { get; set; }
    public virtual ICollection<SysPermission> Children { get; set; } = new List<SysPermission>();
    public virtual ICollection<SysRolePermission> RolePermissions { get; set; } = new List<SysRolePermission>();
}
