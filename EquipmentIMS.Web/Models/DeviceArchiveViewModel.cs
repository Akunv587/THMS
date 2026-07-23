using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class DeviceArchiveViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "序列号不能为空")]
    [MaxLength(100, ErrorMessage = "序列号最多100个字符")]
    [Display(Name = "序列号")]
    public string SerialNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择客户")]
    [Display(Name = "客户")]
    public long CustomerId { get; set; }

    [Required(ErrorMessage = "请选择设备产品")]
    [Display(Name = "设备产品")]
    public long ProductId { get; set; }

    [Display(Name = "关联销售订单")]
    public long? SaleOrderId { get; set; }

    [Display(Name = "安装日期")]
    public DateTime? InstallDate { get; set; }

    [Display(Name = "保修开始日期")]
    public DateTime? WarrantyStart { get; set; }

    [Display(Name = "保修结束日期")]
    public DateTime? WarrantyEnd { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}
