using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_StockTransaction")]
public class InvStockTransaction
{
    public long Id { get; set; }

    [Required, MaxLength(50)]
    public string TransactionNo { get; set; } = string.Empty;

    public TransactionType TransactionType { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public int BalanceQty { get; set; }

    [MaxLength(50)]
    public string? RefType { get; set; }

    public long? RefId { get; set; }

    [MaxLength(50)]
    public string? RefNo { get; set; }

    [Required, MaxLength(50)]
    public string Operator { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remark { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? CreateBy { get; set; }

    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
