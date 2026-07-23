namespace EquipmentIMS.Models.DTOs;

public class DeviceArchiveDto
{
    public long Id { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? SaleOrderId { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? WarrantyStart { get; set; }
    public DateTime? WarrantyEnd { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
