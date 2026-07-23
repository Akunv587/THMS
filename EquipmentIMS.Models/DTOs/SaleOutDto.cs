namespace EquipmentIMS.Models.DTOs;

public class SaleOutDto
{
    public long Id { get; set; }
    public string OutNo { get; set; } = string.Empty;
    public long SaleOrderId { get; set; }
    public string? SaleOrderNo { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateTime OutTime { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public List<SaleOutDetailDto> Details { get; set; } = new();
}
