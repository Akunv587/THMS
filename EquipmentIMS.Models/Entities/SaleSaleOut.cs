using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Sale_SaleOut")]
public class SaleSaleOut : BaseEntity
{
    [Required, MaxLength(50)]
    public string OutNo { get; set; } = string.Empty;

    public long SaleOrderId { get; set; }
    public long CustomerId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime OutTime { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual SaleSaleOrder SaleOrder { get; set; } = null!;
    public virtual BaseCustomer Customer { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
    public virtual ICollection<SaleSaleOutDetail> Details { get; set; } = new List<SaleSaleOutDetail>();
}
