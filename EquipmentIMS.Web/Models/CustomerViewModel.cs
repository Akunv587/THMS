using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class CustomerViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "编码不能为空")]
    [MaxLength(50)]
    [Display(Name = "编码")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(200)]
    [Display(Name = "名称")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "联系人")]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    [Display(Name = "联系电话")]
    public string? ContactPhone { get; set; }

    [MaxLength(100)]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [Display(Name = "邮箱")]
    public string? Email { get; set; }

    [MaxLength(500)]
    [Display(Name = "地址")]
    public string? Address { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "信用额度必须大于等于0")]
    [Display(Name = "信用额度")]
    public decimal CreditLimit { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}

public class CustomerPriceViewModel
{
    public long Id { get; set; }
    public long CustomerId { get; set; }

    [Required(ErrorMessage = "请选择设备")]
    [Display(Name = "设备")]
    public long ProductId { get; set; }

    [Required(ErrorMessage = "销售价不能为空")]
    [Range(0, double.MaxValue, ErrorMessage = "销售价必须大于等于0")]
    [Display(Name = "销售价")]
    public decimal SalePrice { get; set; }

    [Required(ErrorMessage = "生效日期不能为空")]
    [Display(Name = "生效日期")]
    public DateTime EffectiveDate { get; set; } = DateTime.Today;

    [Display(Name = "失效日期")]
    public DateTime? ExpiryDate { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}
