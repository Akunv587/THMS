using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_StockCheckDetail")]
public class InvStockCheckDetail : BaseEntity
{
    public long StockCheckId { get; set; }
    public long ProductId { get; set; }
    public int SystemQty { get; set; }
    public int? ActualQty { get; set; }
    public int DiffQty { get; set; }
    public decimal UnitPrice { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public virtual InvStockCheck StockCheck { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
}
