namespace EquipmentIMS.Models.DTOs;

public class DeviceArchiveListDto
{
    public long Id { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? CustomerName { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public DateTime? WarrantyEnd { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime CreateTime { get; set; }
}
