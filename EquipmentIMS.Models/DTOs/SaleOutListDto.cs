namespace EquipmentIMS.Models.DTOs;

public class SaleOutListDto
{
    public long Id { get; set; }
    public string OutNo { get; set; } = string.Empty;
    public long SaleOrderId { get; set; }
    public string SaleOrderNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OutTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Operator { get; set; }
    public DateTime CreateTime { get; set; }
}
