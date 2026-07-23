namespace EquipmentIMS.Models.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string CreateBy { get; set; } = string.Empty;
    public DateTime? UpdateTime { get; set; }
    public string? UpdateBy { get; set; }
    public bool IsDeleted { get; set; }
}
