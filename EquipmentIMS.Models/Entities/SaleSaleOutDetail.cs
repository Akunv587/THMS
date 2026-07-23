using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sale_SaleOutDetail")]
public class SaleSaleOutDetail : BaseEntity
{
    public long SaleOutId { get; set; }
    public long SaleOrderDetailId { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public virtual SaleSaleOut SaleOut { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
