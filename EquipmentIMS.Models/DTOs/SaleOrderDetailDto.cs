namespace EquipmentIMS.Models.DTOs;

public class SaleOrderDetailDto
{
    public long Id { get; set; }
    public long SaleOrderId { get; set; }
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int Qty { get; set; }
    public int ShippedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Remark { get; set; }
}
