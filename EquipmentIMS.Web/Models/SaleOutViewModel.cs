using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class SaleOutViewModel
{
    [Required(ErrorMessage = "销售订单必选")]
    public long SaleOrderId { get; set; }

    public string? SaleOrderNo { get; set; }
    public string? CustomerName { get; set; }

    [Required(ErrorMessage = "出库日期必填")]
    public DateTime OutTime { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? Operator { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public List<SaleOutDetailRowViewModel> Details { get; set; } = new();
}

public class SaleOutDetailRowViewModel
{
    public long SaleOrderDetailId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int OrderQty { get; set; }
    public int ShippedQty { get; set; }
    public int RemainingQty => OrderQty - ShippedQty;
    public int AvailableStock { get; set; }
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "出库数量必须大于0")]
    public int OutQty { get; set; }

    public decimal Amount => OutQty * UnitPrice;
}

public class SaleOutQueryViewModel
{
    public string? Keyword { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
