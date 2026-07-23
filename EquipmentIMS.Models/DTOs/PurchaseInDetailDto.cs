namespace EquipmentIMS.Models.DTOs;

public class PurchaseInDetailDto
{
    public long Id { get; set; }
    public long PurchaseInId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
