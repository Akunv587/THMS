using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_StockAlert")]
public class InvStockAlert
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public AlertLevel AlertLevel { get; set; }
    public int CurrentQty { get; set; }
    public int MinStock { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;

    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
