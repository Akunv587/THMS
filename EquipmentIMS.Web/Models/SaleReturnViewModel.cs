using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class SaleReturnViewModel
{
    [Required(ErrorMessage = "出库单必选")]
    public long SaleOutId { get; set; }

    public string? SaleOutNo { get; set; }
    public string? CustomerName { get; set; }

    [Required(ErrorMessage = "退货日期必填")]
    public DateTime ReturnDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "退货原因必填")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public List<SaleReturnDetailRowViewModel> Details { get; set; } = new();
}

public class SaleReturnDetailRowViewModel
{
    public long SaleOutDetailId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int OutQty { get; set; }
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "退货数量必须大于0")]
    public int ReturnQty { get; set; }

    public decimal Amount => ReturnQty * UnitPrice;
}

public class SaleReturnQueryViewModel
{
    public string? Keyword { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Status { get; set; }
}
