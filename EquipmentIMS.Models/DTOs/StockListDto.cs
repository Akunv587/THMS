namespace EquipmentIMS.Models.DTOs;

public class StockListDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string? Brand { get; set; }
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int LockQty { get; set; }
    public int AvailableQty { get; set; }
    public int MinStock { get; set; }
    public string WarningLevel { get; set; } = "正常";
}
