using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Maint_RepairOrder")]
public class MaintRepairOrder : BaseEntity
{
    [Required, MaxLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    public long DeviceArchiveId { get; set; }
    public long CustomerId { get; set; }

    [Required, MaxLength(1000)]
    public string FaultDesc { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FaultType { get; set; }

    public RepairOrderStatus Status { get; set; }
    public long? TechnicianId { get; set; }

    [MaxLength(50)]
    public string? TechnicianName { get; set; }

    public DateTime? AssignTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }

    [MaxLength(2000)]
    public string? RepairDesc { get; set; }

    public decimal WorkHours { get; set; }
    public decimal RepairCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsWarranty { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual MaintDeviceArchive DeviceArchive { get; set; } = null!;
    public virtual BaseCustomer Customer { get; set; } = null!;
    public virtual SysUser? Technician { get; set; }
    public virtual ICollection<MaintRepairPart> RepairParts { get; set; } = new List<MaintRepairPart>();
}
