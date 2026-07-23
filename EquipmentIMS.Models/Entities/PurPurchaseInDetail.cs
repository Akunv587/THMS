using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Pur_PurchaseInDetail")]
public class PurPurchaseInDetail : BaseEntity
{
    public long PurchaseInId { get; set; }
    public long ProductId { get; set; }
    public int Qty { get; set; }
    public int ReturnedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public virtual PurPurchaseIn PurchaseIn { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
}
