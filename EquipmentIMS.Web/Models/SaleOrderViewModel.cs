using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class SaleOrderViewModel
{
    public long Id { get; set; }
    public string? OrderNo { get; set; }

    [Required(ErrorMessage = "客户必选")]
    [Display(Name = "客户")]
    public long CustomerId { get; set; }

    [Required(ErrorMessage = "订单日期必填")]
    [Display(Name = "订单日期")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Display(Name = "预计交货日期")]
    public DateTime? ExpectedDate { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }

    public List<SaleOrderDetailRowViewModel> Details { get; set; } = new();
}

public class SaleOrderDetailRowViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "设备必选")]
    public string ProductId { get; set; } = string.Empty;

    public string? ProductText { get; set; }

    [Required(ErrorMessage = "出库仓库必选")]
    public string WarehouseId { get; set; } = string.Empty;

    [Required(ErrorMessage = "数量必填")]
    [Range(1, int.MaxValue, ErrorMessage = "数量必须大于0")]
    public int Qty { get; set; } = 1;

    [Required(ErrorMessage = "单价必填")]
    [Range(0, double.MaxValue, ErrorMessage = "单价不能为负数")]
    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? Remark { get; set; }
}

public class SaleOrderQueryViewModel
{
    public string? Keyword { get; set; }
    public long? CustomerId { get; set; }
    public int? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
