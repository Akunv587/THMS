using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_RolePermission")]
public class SysRolePermission
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    public virtual SysRole Role { get; set; } = null!;
    public virtual SysPermission Permission { get; set; } = null!;
}
