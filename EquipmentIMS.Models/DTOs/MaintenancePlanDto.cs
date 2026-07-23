namespace EquipmentIMS.Models.DTOs;

public class MaintenancePlanDto
{
    public long Id { get; set; }
    public long DeviceArchiveId { get; set; }
    public string? SerialNo { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int CycleType { get; set; }
    public int CycleDays { get; set; }
    public DateTime NextDate { get; set; }
    public string? Content { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
