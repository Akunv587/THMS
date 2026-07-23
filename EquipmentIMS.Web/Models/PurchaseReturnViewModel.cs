using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class PurchaseReturnViewModel
{
    [Required(ErrorMessage = "请选择入库单")]
    [Range(1, long.MaxValue, ErrorMessage = "请选择入库单")]
    public long PurchaseInId { get; set; }

    [Required(ErrorMessage = "请选择退货日期")]
    public DateTime ReturnDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    [Required(ErrorMessage = "请添加退货明细")]
    public List<PurchaseReturnDetailViewModel> Details { get; set; } = new();
}

public class PurchaseReturnDetailViewModel
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int InQty { get; set; }
    public int ReturnedQty { get; set; }
    public int RemainingQty { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "退货数量必须大于0")]
    public int ReturnQty { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class PurchaseReturnQueryViewModel
{
    public string? Keyword { get; set; }
    public long? SupplierId { get; set; }
    public long? WarehouseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
