using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_BackupHistory")]
public class InvBackupHistory : BaseEntity
{
    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public bool IsVerified { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
}
