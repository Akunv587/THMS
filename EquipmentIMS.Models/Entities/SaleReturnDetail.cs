using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sale_ReturnDetail")]
public class SaleReturnDetail : BaseEntity
{
    public long SaleReturnId { get; set; }
    public long SaleOutDetailId { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public virtual SaleReturn SaleReturn { get; set; } = null!;
    public virtual SaleSaleOutDetail SaleOutDetail { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
}
