namespace EquipmentIMS.Models.DTOs;

/// <summary>
/// 入库请求 DTO（仅用于入库操作的入参，不同于 Mapster 映射的 PurchaseInDto）
/// </summary>
public class PurchaseInRequestDto
{
    public long PurchaseOrderId { get; set; }
    public DateTime InDate { get; set; }
    public string? Remark { get; set; }
    public List<PurchaseInItemRequestDto> Details { get; set; } = new();
}

/// <summary>
/// 入库明细请求项（含订单上下文：订单数量、已入库、剩余等）
/// </summary>
public class PurchaseInItemRequestDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int OrderQty { get; set; }
    public int ReceivedQty { get; set; }
    public int RemainingQty { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
