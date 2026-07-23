using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class RepairOrderViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "请选择设备档案")]
    [Display(Name = "设备档案")]
    public long DeviceArchiveId { get; set; }

    [Required(ErrorMessage = "故障描述不能为空")]
    [MaxLength(1000, ErrorMessage = "故障描述最多1000个字符")]
    [Display(Name = "故障描述")]
    public string FaultDesc { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "故障类型")]
    public string? FaultType { get; set; }

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}

public class DispatchViewModel
{
    public long OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择技术员")]
    [Display(Name = "指派技术员")]
    public long TechnicianId { get; set; }
}

public class ProcessViewModel
{
    public long OrderId { get; set; }

    [Required(ErrorMessage = "维修描述不能为空")]
    [MaxLength(2000)]
    [Display(Name = "维修描述")]
    public string RepairDesc { get; set; } = string.Empty;

    [Range(0, 9999)]
    [Display(Name = "工时(小时)")]
    public decimal WorkHours { get; set; }

    [Range(0, 99999999)]
    [Display(Name = "维修费")]
    public decimal RepairCost { get; set; }
}

public class RepairPartInputModel
{
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int Qty { get; set; }
}
