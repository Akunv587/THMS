using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Maint_DeviceArchive")]
public class MaintDeviceArchive : BaseEntity
{
    [Required, MaxLength(100)]
    public string SerialNo { get; set; } = string.Empty;

    public long ProductId { get; set; }
    public long CustomerId { get; set; }
    public long? SaleOrderId { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? WarrantyStart { get; set; }
    public DateTime? WarrantyEnd { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Normal;

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseCustomer Customer { get; set; } = null!;
    public virtual SaleSaleOrder? SaleOrder { get; set; }
    public virtual ICollection<MaintRepairOrder> RepairOrders { get; set; } = new List<MaintRepairOrder>();
    public virtual ICollection<MaintMaintenancePlan> MaintenancePlans { get; set; } = new List<MaintMaintenancePlan>();
}
