using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class CategoryViewModel
{
    public long Id { get; set; }

    [Display(Name = "上级分类")]
    public long? ParentId { get; set; }

    [Required(ErrorMessage = "编码不能为空")]
    [MaxLength(50)]
    [Display(Name = "编码")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(100)]
    [Display(Name = "名称")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "排序")]
    public int SortOrder { get; set; }
}
