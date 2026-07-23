using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Sale_Return")]
public class SaleReturn : BaseEntity
{
    [Required, MaxLength(50)]
    public string ReturnNo { get; set; } = string.Empty;

    public long SaleOutId { get; set; }
    public long CustomerId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public SaleReturnStatus Status { get; set; }

    [MaxLength(50)]
    public string? Approver { get; set; }

    public DateTime? ApproveTime { get; set; }

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual SaleSaleOut SaleOut { get; set; } = null!;
    public virtual BaseCustomer Customer { get; set; } = null!;
    public virtual BaseWarehouse Warehouse { get; set; } = null!;
    public virtual ICollection<SaleReturnDetail> Details { get; set; } = new List<SaleReturnDetail>();
}
