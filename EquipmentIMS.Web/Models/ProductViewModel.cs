using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EquipmentIMS.Web.Models;

public class ProductViewModel
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

    [Required(ErrorMessage = "请选择分类")]
    [Display(Name = "设备分类")]
    public long CategoryId { get; set; }

    [MaxLength(200)]
    [Display(Name = "规格型号")]
    public string? Spec { get; set; }

    [MaxLength(100)]
    [Display(Name = "品牌")]
    public string? Brand { get; set; }

    [MaxLength(20)]
    [Display(Name = "单位")]
    public string? Unit { get; set; }

    [MaxLength(100)]
    [Display(Name = "条码")]
    public string? BarCode { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "采购价必须大于等于0")]
    [Display(Name = "采购价")]
    public decimal PurchasePrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "销售价必须大于等于0")]
    [Display(Name = "销售价")]
    public decimal SalePrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "最低库存必须大于等于0")]
    [Display(Name = "最低库存")]
    public int MinStock { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}

public class ProductImportViewModel
{
    [Required(ErrorMessage = "请选择Excel文件")]
    [Display(Name = "Excel文件")]
    public IFormFile? File { get; set; }
}
