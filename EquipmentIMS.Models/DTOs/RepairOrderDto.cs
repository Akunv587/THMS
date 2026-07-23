namespace EquipmentIMS.Models.DTOs;

public class RepairOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long DeviceArchiveId { get; set; }
    public string? SerialNo { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string FaultDesc { get; set; } = string.Empty;
    public string? FaultType { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public long? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? RepairDesc { get; set; }
    public decimal WorkHours { get; set; }
    public decimal RepairCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsWarranty { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public List<RepairPartDto> Parts { get; set; } = new();
}
