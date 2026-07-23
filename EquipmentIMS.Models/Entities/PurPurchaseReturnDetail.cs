using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Pur_PurchaseReturnDetail")]
public class PurPurchaseReturnDetail : BaseEntity
{
    public long PurchaseReturnId { get; set; }
    public long ProductId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public virtual PurPurchaseReturn PurchaseReturn { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
}
