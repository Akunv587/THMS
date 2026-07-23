namespace EquipmentIMS.Models.DTOs;

public class SaleReturnDetailDto
{
    public long SaleOutDetailId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ReturnQty { get; set; }
    public int OutQty { get; set; }
    public decimal UnitPrice { get; set; }
}
