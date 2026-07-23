namespace EquipmentIMS.Models.DTOs;

public class SaleOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippedAmount { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? Approver { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string? Remark { get; set; }
    public string? CreateBy { get; set; }
    public DateTime CreateTime { get; set; }
    public List<SaleOrderDetailDto> Details { get; set; } = new();
}
