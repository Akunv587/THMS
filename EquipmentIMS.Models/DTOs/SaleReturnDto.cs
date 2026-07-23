namespace EquipmentIMS.Models.DTOs;

public class SaleReturnDto
{
    public long Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public long SaleOutId { get; set; }
    public string? SaleOutNo { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long WarehouseId { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Approver { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public List<SaleReturnDetailDto> Details { get; set; } = new();
}
