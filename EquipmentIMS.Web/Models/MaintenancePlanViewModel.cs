using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.Models;

public class MaintenancePlanViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "请选择设备档案")]
    [Display(Name = "设备档案")]
    public long DeviceArchiveId { get; set; }

    [Required(ErrorMessage = "计划名称不能为空")]
    [MaxLength(200)]
    [Display(Name = "计划名称")]
    public string PlanName { get; set; } = string.Empty;

    [Display(Name = "周期类型")]
    public int CycleType { get; set; } = 1;

    [Display(Name = "周期天数")]
    public int CycleDays { get; set; } = 30;

    [Required(ErrorMessage = "下次保养日期不能为空")]
    [Display(Name = "下次保养日期")]
    public DateTime NextDate { get; set; } = DateTime.Today.AddMonths(1);

    [MaxLength(2000)]
    [Display(Name = "保养内容")]
    public string? Content { get; set; }

    [Display(Name = "启用")]
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    [Display(Name = "备注")]
    public string? Remark { get; set; }
}
