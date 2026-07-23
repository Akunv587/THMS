using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Maint_RepairPart")]
public class MaintRepairPart : BaseEntity
{
    public long RepairOrderId { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public virtual MaintRepairOrder RepairOrder { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
