using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class PurchaseOrderViewModel
{
    public long Id { get; set; }
    public string? OrderNo { get; set; }

    [Required(ErrorMessage = "供应商必选")]
    [Display(Name = "供应商")]
    public long SupplierId { get; set; }

    [Required(ErrorMessage = "仓库必选")]
    [Display(Name = "仓库")]
    public long WarehouseId { get; set; }

    [Required(ErrorMessage = "订单日期必填")]
    [Display(Name = "订单日期")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Display(Name = "预计到货日期")]
    public DateTime? ExpectedDate { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }

    public List<PurchaseOrderDetailRowViewModel> Details { get; set; } = new();
}

public class PurchaseOrderDetailRowViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "设备必选")]
    public string ProductId { get; set; } = string.Empty;

    public string? ProductText { get; set; }

    [Required(ErrorMessage = "数量必填")]
    [Range(1, int.MaxValue, ErrorMessage = "数量必须大于0")]
    public int Qty { get; set; } = 1;

    [Required(ErrorMessage = "单价必填")]
    [Range(0, double.MaxValue, ErrorMessage = "单价不能为负数")]
    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? Remark { get; set; }
}

public class PurchaseOrderQueryViewModel
{
    public string? Keyword { get; set; }
    public long? SupplierId { get; set; }
    public long? WarehouseId { get; set; }
    public int? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
