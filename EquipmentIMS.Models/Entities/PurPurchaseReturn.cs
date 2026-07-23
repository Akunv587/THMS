using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Pur_PurchaseReturn")]
public class PurPurchaseReturn : BaseEntity
{
    [Required, MaxLength(50)]
    public string ReturnNo { get; set; } = string.Empty;

    public long PurchaseInId { get; set; }
    public long SupplierId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public PurchaseReturnStatus Status { get; set; }

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual PurPurchaseIn PurchaseIn { get; set; } = null!;
    public virtual BaseSupplier Supplier { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
    public virtual ICollection<PurPurchaseReturnDetail> Details { get; set; } = new List<PurPurchaseReturnDetail>();
}
