namespace EquipmentIMS.Models.DTOs;

public class RepairOrderListDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? SerialNo { get; set; }
    public string? ProductName { get; set; }
    public string? CustomerName { get; set; }
    public string FaultDesc { get; set; } = string.Empty;
    public string? FaultType { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public decimal RepairCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsWarranty { get; set; }
    public DateTime CreateTime { get; set; }
}
