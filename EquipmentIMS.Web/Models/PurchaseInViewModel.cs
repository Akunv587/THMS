using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class PurchaseInViewModel
{
    [Required(ErrorMessage = "请选择采购订单")]
    [Range(1, long.MaxValue, ErrorMessage = "请选择采购订单")]
    public long PurchaseOrderId { get; set; }

    [Required(ErrorMessage = "请选择入库日期")]
    public DateTime InDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Remark { get; set; }

    [Required(ErrorMessage = "请添加入库明细")]
    public List<PurchaseInDetailViewModel> Details { get; set; } = new();
}

public class PurchaseInDetailViewModel
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int OrderQty { get; set; }
    public int ReceivedQty { get; set; }
    public int RemainingQty { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "入库数量必须大于0")]
    public int InQty { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class PurchaseInQueryViewModel
{
    public string? Keyword { get; set; }
    public long? SupplierId { get; set; }
    public long? WarehouseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
