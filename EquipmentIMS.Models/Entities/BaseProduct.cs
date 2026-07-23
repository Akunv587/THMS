using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Base_Product")]
public class BaseProduct : BaseEntity
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public long CategoryId { get; set; }

    [MaxLength(200)]
    public string? Spec { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? BarCode { get; set; }

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int MinStock { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseCategory Category { get; set; } = null!;
    public virtual ICollection<BaseProductStock> ProductStocks { get; set; } = new List<BaseProductStock>();
}
