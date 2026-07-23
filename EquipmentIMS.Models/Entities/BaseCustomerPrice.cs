using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Base_CustomerPrice")]
public class BaseCustomerPrice : BaseEntity
{
    public long CustomerId { get; set; }
    public long ProductId { get; set; }
    public decimal SalePrice { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual BaseCustomer Customer { get; set; } = null!;
    public virtual BaseProduct Product { get; set; } = null!;
}
