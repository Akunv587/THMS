namespace EquipmentIMS.Models.DTOs;

public class PurchaseOrderDetailDto
{
    public long Id { get; set; }
    public long PurchaseOrderId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int Qty { get; set; }
    public int ReceivedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Remark { get; set; }
}
