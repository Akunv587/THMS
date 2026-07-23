using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.DTOs;

public class SaleOrderListDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippedAmount { get; set; }
    public SaleOrderStatus Status { get; set; }
    public string? StatusName => Status switch
    {
        SaleOrderStatus.Draft => "草稿",
        SaleOrderStatus.PendingApproval => "待审批",
        SaleOrderStatus.Approved => "已审批",
        SaleOrderStatus.PartialShipped => "部分出库",
        SaleOrderStatus.Shipped => "已出库",
        SaleOrderStatus.Returned => "已退货",
        SaleOrderStatus.Cancelled => "已取消",
        _ => ""
    };
    public string? CreateBy { get; set; }
    public DateTime CreateTime { get; set; }
}

public class SaleOrderQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public long? CustomerId { get; set; }
    public SaleOrderStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class SaleOrderDetailOutDto
{
    public long SaleOrderDetailId { get; set; }
    public long SaleOrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int OrderQty { get; set; }
    public int ShippedQty { get; set; }
    public int RemainingQty => OrderQty - ShippedQty;
    public int AvailableStock { get; set; }
    public int LockQty { get; set; }
    public decimal UnitPrice { get; set; }
}
