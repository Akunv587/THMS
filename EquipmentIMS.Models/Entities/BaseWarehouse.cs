using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Base_Warehouse")]
public class BaseWarehouse : BaseEntity
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Manager { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsDefault { get; set; }

    public virtual ICollection<BaseProductStock> ProductStocks { get; set; } = new List<BaseProductStock>();
}
