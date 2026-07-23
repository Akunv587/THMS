using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Pur_PurchaseOrder")]
public class PurPurchaseOrder : BaseEntity
{
    [Required, MaxLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    public long SupplierId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }

    [MaxLength(50)]
    public string? Approver { get; set; }

    public DateTime? ApproveTime { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseSupplier Supplier { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
    public virtual ICollection<PurPurchaseOrderDetail> Details { get; set; } = new List<PurPurchaseOrderDetail>();
}
