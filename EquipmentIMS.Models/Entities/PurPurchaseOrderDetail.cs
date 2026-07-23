using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Pur_PurchaseOrderDetail")]
public class PurPurchaseOrderDetail : BaseEntity
{
    public long PurchaseOrderId { get; set; }
    public long ProductId { get; set; }
    public int Qty { get; set; }
    public int ReceivedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public virtual PurPurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
}
