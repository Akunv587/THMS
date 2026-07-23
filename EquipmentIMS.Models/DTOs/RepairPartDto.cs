namespace EquipmentIMS.Models.DTOs;

public class RepairPartDto
{
    public long Id { get; set; }
    public long RepairOrderId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Remark { get; set; }
}
