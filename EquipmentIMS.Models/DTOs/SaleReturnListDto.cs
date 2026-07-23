namespace EquipmentIMS.Models.DTOs;

public class SaleReturnListDto
{
    public long Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public string SaleOutNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Approver { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}
