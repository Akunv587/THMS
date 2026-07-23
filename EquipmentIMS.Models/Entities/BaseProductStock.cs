using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Base_ProductStock")]
public class BaseProductStock : BaseEntity
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int LockQty { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
