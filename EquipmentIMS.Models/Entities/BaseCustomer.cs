using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipmentIMS.Models.Entities;

[Table("Base_Customer")]
public class BaseCustomer : BaseEntity
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public decimal CreditLimit { get; set; }
    public decimal UsedCredit { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public virtual ICollection<SaleSaleOrder> SaleOrders { get; set; } = new List<SaleSaleOrder>();
    public virtual ICollection<BaseCustomerPrice> CustomerPrices { get; set; } = new List<BaseCustomerPrice>();
}
