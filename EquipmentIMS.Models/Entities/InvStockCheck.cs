using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_StockCheck")]
public class InvStockCheck : BaseEntity
{
    [Required, MaxLength(50)]
    public string CheckNo { get; set; } = string.Empty;

    public long WarehouseId { get; set; }
    public DateTime CheckDate { get; set; }
    public StockCheckStatus Status { get; set; }
    public bool IsLocked { get; set; }
    public decimal TotalDiffAmount { get; set; }

    [MaxLength(50)]
    public string? Approver { get; set; }

    public DateTime? ApproveTime { get; set; }

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseWarehouse Warehouse { get; set; } = null!;
    public virtual ICollection<InvStockCheckDetail> Details { get; set; } = new List<InvStockCheckDetail>();
}
