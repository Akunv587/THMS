using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_User")]
public class SysUser : BaseEntity
{
    [Required, MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string RealName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PasswordSalt { get; set; } = string.Empty;

    public bool MustChangePwd { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginTime { get; set; }

    [MaxLength(50)]
    public string? LastLoginIP { get; set; }

    public DateTime? LastPasswordChanged { get; set; }

    public virtual ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
}
