using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Models.DTOs;

public class PurchaseInListDto
{
    public long Id { get; set; }
    public string InNo { get; set; } = string.Empty;
    public long PurchaseOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime InDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseInStatus Status { get; set; }
    public string StatusName => Status switch
    {
        PurchaseInStatus.PendingIn => "待入库",
        PurchaseInStatus.Received => "已入库",
        PurchaseInStatus.Returned => "已退货",
        PurchaseInStatus.Cancelled => "已取消",
        _ => Status.ToString()
    };
    public string Operator { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 入库单详情 DTO（Mapster 映射自 PurPurchaseIn 实体，用于详情/列表展示）
/// </summary>
public class PurchaseInDto
{
    public long Id { get; set; }
    public string InNo { get; set; } = string.Empty;
    public long PurchaseOrderId { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime InDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public List<PurchaseInDetailDto> Details { get; set; } = new();
}

public class PurchaseInQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public long? SupplierId { get; set; }
    public long? WarehouseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
