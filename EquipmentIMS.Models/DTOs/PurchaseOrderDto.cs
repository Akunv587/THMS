using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.DTOs;

public class PurchaseOrderListDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public string StatusName => GetStatusName(Status);
    public string CreateBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }

    public static string GetStatusName(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "草稿",
        PurchaseOrderStatus.PendingApproval => "待审批",
        PurchaseOrderStatus.Approved => "已审批",
        PurchaseOrderStatus.PartialReceived => "部分入库",
        PurchaseOrderStatus.Received => "已入库",
        PurchaseOrderStatus.Closed => "已关闭",
        PurchaseOrderStatus.Cancelled => "已取消",
        _ => status.ToString()
    };
}




public class PurchaseOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public string StatusName => PurchaseOrderListDto.GetStatusName(Status);
    public string? Approver { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string? Remark { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public List<PurchaseOrderDetailDto> Details { get; set; } = new();
}

public class PurchaseOrderQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public long? SupplierId { get; set; }
    public long? WarehouseId { get; set; }
    public PurchaseOrderStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
