using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class SupplierViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "编码不能为空")]
    [MaxLength(50, ErrorMessage = "编码最多50个字符")]
    [Display(Name = "编码")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(200, ErrorMessage = "名称最多200个字符")]
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

    [MaxLength(500)]
    [Display(Name = "银行账号")]
    public string? BankAccount { get; set; }

    [MaxLength(100)]
    [Display(Name = "开户行")]
    public string? BankName { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}
