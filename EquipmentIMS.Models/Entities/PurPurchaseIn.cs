using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Pur_PurchaseIn")]
public class PurPurchaseIn : BaseEntity
{
    [Required, MaxLength(50)]
    public string InNo { get; set; } = string.Empty;

    public long PurchaseOrderId { get; set; }
    public long SupplierId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime InDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseInStatus Status { get; set; }

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual PurPurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual BaseSupplier Supplier { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
    public virtual ICollection<PurPurchaseInDetail> Details { get; set; } = new List<PurPurchaseInDetail>();
}
