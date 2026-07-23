using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.Entities;

[Table("Inv_StockTransfer")]
public class InvStockTransfer : BaseEntity
{
    [Required, MaxLength(50)]
    public string TransferNo { get; set; } = string.Empty;

    public long FromWarehouseId { get; set; }
    public long ToWarehouseId { get; set; }
    public DateTime TransferDate { get; set; }
    public TransferStatus Status { get; set; }

    [MaxLength(50)]
    public string? Approver { get; set; }

    public DateTime? ApproveTime { get; set; }

    [MaxLength(500)]
    public string? RejectReason { get; set; }

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseWarehouse FromWarehouse { get; set; } = null!;
    public virtual BaseWarehouse ToWarehouse { get; set; } = null!;
    public virtual ICollection<InvStockTransferDetail> Details { get; set; } = new List<InvStockTransferDetail>();
}
