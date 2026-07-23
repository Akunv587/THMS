using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Sale_SaleOrder")]
public class SaleSaleOrder : BaseEntity
{
    [Required, MaxLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    public long CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippedAmount { get; set; }
    public SaleOrderStatus Status { get; set; }

    [MaxLength(50)]
    public string? Approver { get; set; }

    public DateTime? ApproveTime { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseCustomer Customer { get; set; } = null!;
    public virtual ICollection<SaleSaleOrderDetail> Details { get; set; } = new List<SaleSaleOrderDetail>();
}
