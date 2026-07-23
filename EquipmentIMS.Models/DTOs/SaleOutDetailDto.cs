namespace EquipmentIMS.Models.DTOs;

public class SaleOutDetailDto
{
    public long Id { get; set; }
    public long SaleOutId { get; set; }
    public long SaleOrderDetailId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSpec { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
