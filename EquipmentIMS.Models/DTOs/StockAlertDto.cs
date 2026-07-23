namespace EquipmentIMS.Models.DTOs;

public class StockAlertWarningDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int CurrentQty { get; set; }
    public int AvailableQty { get; set; }
    public int MinStock { get; set; }
    public int AlertLevel { get; set; }
    public string AlertLevelName { get; set; } = string.Empty;
}

public class StockAlertRecordDto
{
    public long Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int AlertLevel { get; set; }
    public string AlertLevelName { get; set; } = string.Empty;
    public int CurrentQty { get; set; }
    public int MinStock { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public DateTime CreateTime { get; set; }
}

public class StockAlertQueryDto
{
    public bool? IsResolved { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>库存预警统计</summary>
public class StockWarningStatDto
{
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int NormalCount { get; set; }
    public int TotalProductCount { get; set; }
}

/// <summary>按分类的预警分布（供柱状图）</summary>
public class CategoryWarningDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int TotalCount { get; set; }
}
