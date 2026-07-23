namespace EquipmentIMS.Models.DTOs.Reports;

public class InventoryReportDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Brand { get; set; }
    public string? CategoryName { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int LockQty { get; set; }
    public int AvailableQty { get; set; }
    public int MinStock { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsWarning { get; set; }
}

public class StockFundDto
{
    public string GroupName { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty; // "warehouse" / "category"
    public decimal StockValue { get; set; }
    public int ProductCount { get; set; }
    public int TotalQty { get; set; }
    public decimal Percent { get; set; }
}

public class StockWarningDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int CurrentQty { get; set; }
    public int MinStock { get; set; }
    public int Gaps { get; set; }
    public string AlertLevel { get; set; } = string.Empty; // "严重不足" / "低于安全库存"
    public DateTime CreateTime { get; set; }
}
