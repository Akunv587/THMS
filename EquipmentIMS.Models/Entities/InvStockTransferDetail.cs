using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_StockTransferDetail")]
public class InvStockTransferDetail : BaseEntity
{
    public long TransferId { get; set; }
    public long ProductId { get; set; }
    public int Qty { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public virtual InvStockTransfer Transfer { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
}
