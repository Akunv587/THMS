using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_UserRole")]
public class SysUserRole
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }

    public virtual SysUser User { get; set; } = null!;
    public virtual SysRole Role { get; set; } = null!;
}
