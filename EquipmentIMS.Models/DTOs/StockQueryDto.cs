namespace EquipmentIMS.Models.DTOs;

public class StockQueryDto
{
    // 查询参数
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public long? CategoryId { get; set; }
    public long? WarehouseId { get; set; }
    public bool? WarningOnly { get; set; }

    // 输出字段（列表使用）
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Spec { get; set; }
    public string? Brand { get; set; }
    public string? CategoryName { get; set; }
    public string? WarehouseName { get; set; }
    public int Quantity { get; set; }
    public int LockQty { get; set; }
    public int AvailableQty { get; set; }
    public int MinStock { get; set; }
    public string? WarningLevel { get; set; }
}
