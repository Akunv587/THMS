using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sys_PasswordHistory")]
public class SysPasswordHistory
{
    public long Id { get; set; }
    public long UserId { get; set; }

    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PasswordSalt { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public virtual SysUser User { get; set; } = null!;
}
