namespace EquipmentIMS.Models.DTOs;

public class InitialStockDto
{
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int Quantity { get; set; }
    public string? Remark { get; set; }
}
