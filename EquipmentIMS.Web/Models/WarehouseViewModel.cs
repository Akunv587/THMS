using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class WarehouseViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "编码不能为空")]
    [MaxLength(50)]
    [Display(Name = "编码")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(100)]
    [Display(Name = "名称")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "地址")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [Display(Name = "负责人")]
    public string? Manager { get; set; }

    [MaxLength(20)]
    [Display(Name = "联系电话")]
    public string? Phone { get; set; }

    [Display(Name = "默认仓库")]
    public bool IsDefault { get; set; }
}
