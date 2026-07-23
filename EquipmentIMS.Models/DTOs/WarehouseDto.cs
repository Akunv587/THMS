namespace EquipmentIMS.Models.DTOs;

public class WarehouseDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Manager { get; set; }
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreateTime { get; set; }
}
