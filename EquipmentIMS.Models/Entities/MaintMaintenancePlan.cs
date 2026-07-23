using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Maint_MaintenancePlan")]
public class MaintMaintenancePlan : BaseEntity
{
    public long DeviceArchiveId { get; set; }

    [Required, MaxLength(200)]
    public string PlanName { get; set; } = string.Empty;

    public CycleType CycleType { get; set; }
    public int CycleDays { get; set; }
    public DateTime NextDate { get; set; }

    [MaxLength(2000)]
    public string? Content { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual MaintDeviceArchive DeviceArchive { get; set; } = null!;
}
