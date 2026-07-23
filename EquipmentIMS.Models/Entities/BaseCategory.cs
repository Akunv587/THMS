using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Base_Category")]
public class BaseCategory : BaseEntity
{
    public long? ParentId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; } = 1;
    public int SortOrder { get; set; }

    public virtual BaseCategory? Parent { get; set; }
    public virtual ICollection<BaseCategory> Children { get; set; } = new List<BaseCategory>();
    public virtual ICollection<BaseProduct> Products { get; set; } = new List<BaseProduct>();
}
