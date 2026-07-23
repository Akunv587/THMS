using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sale_SaleOrderDetail")]
public class SaleSaleOrderDetail : BaseEntity
{
    public long SaleOrderId { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public int ShippedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public virtual SaleSaleOrder SaleOrder { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
