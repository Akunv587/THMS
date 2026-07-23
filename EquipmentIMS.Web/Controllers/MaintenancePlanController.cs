using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("maint:plan:view")]
public class MaintenancePlanController : Controller
{
    private readonly IMaintenanceService _service;
    private readonly IDeviceArchiveService _archiveService;
    private readonly ICurrentUserService _user;

    public MaintenancePlanController(IMaintenanceService service, IDeviceArchiveService archiveService, ICurrentUserService user)
    {
        _service = service;
        _archiveService = archiveService;
        _user = user;
    }

    public async Task<IActionResult> Index(long? deviceArchiveId, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetPlansAsync(deviceArchiveId, page, pageSize);
        ViewData["DeviceArchiveId"] = deviceArchiveId;

        ViewData["CycleTypeList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "", Text = "全部" },
            new() { Value = "1", Text = "每月" },
            new() { Value = "2", Text = "每季" },
            new() { Value = "3", Text = "每半年" },
            new() { Value = "4", Text = "每年" }
        }, "Value", "Text");

        return View(result);
    }

    [PermissionFilter("maint:plan:create")]
    public IActionResult Create()
    {
        ViewData["CycleTypeList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "1", Text = "每月" },
            new() { Value = "2", Text = "每季" },
            new() { Value = "3", Text = "每半年" },
            new() { Value = "4", Text = "每年" }
        }, "Value", "Text");

        return View(new MaintenancePlanViewModel());
    }

    [HttpPost]
    [PermissionFilter("maint:plan:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenancePlanViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var dto = new MaintenancePlanDto
        {
            DeviceArchiveId = vm.DeviceArchiveId,
            PlanName = vm.PlanName,
            CycleType = vm.CycleType,
            CycleDays = vm.CycleDays,
            NextDate = vm.NextDate,
            Content = vm.Content,
            IsActive = vm.IsActive,
            Remark = vm.Remark
        };
        var id = await _service.CreatePlanAsync(dto, _user.RealName);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("maint:plan:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetPlanByIdAsync(id);
        if (dto == null) return NotFound();

        ViewData["CycleTypeList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "1", Text = "每月" },
            new() { Value = "2", Text = "每季" },
            new() { Value = "3", Text = "每半年" },
            new() { Value = "4", Text = "每年" }
        }, "Value", "Text", dto.CycleType.ToString());

        var vm = new MaintenancePlanViewModel
        {
            Id = dto.Id,
            DeviceArchiveId = dto.DeviceArchiveId,
            PlanName = dto.PlanName,
            CycleType = dto.CycleType,
            CycleDays = dto.CycleDays,
            NextDate = dto.NextDate,
            Content = dto.Content,
            IsActive = dto.IsActive,
            Remark = dto.Remark
        };
        return View("Create", vm);
    }

    [HttpPost]
    [PermissionFilter("maint:plan:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MaintenancePlanViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("Create", vm);

        var dto = new MaintenancePlanDto
        {
            Id = vm.Id,
            DeviceArchiveId = vm.DeviceArchiveId,
            PlanName = vm.PlanName,
            CycleType = vm.CycleType,
            CycleDays = vm.CycleDays,
            NextDate = vm.NextDate,
            Content = vm.Content,
            IsActive = vm.IsActive,
            Remark = vm.Remark
        };
        await _service.UpdatePlanAsync(dto, _user.RealName);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionFilter("maint:plan:delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeletePlanAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SearchDeviceArchives(string term)
    {
        var result = await _archiveService.GetListAsync(null, null, null, 1, 100);
        var filtered = string.IsNullOrWhiteSpace(term)
            ? result.Items
            : result.Items.Where(a => a.SerialNo != null && a.SerialNo.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        var items = filtered.Select(a => new { id = a.Id, text = $"{a.SerialNo} - {a.ProductName}（{a.CustomerName}）" });
        return Json(new { results = items });
    }

    [HttpGet]
    public async Task<IActionResult> GetArchiveInfo(long id)
    {
        var archive = await _archiveService.GetByIdAsync(id);
        if (archive == null) return Json(ApiResponse.Fail("设备档案不存在", "NOT_FOUND"));
        return Json(ApiResponse.Ok(new
        {
            archive.Id,
            archive.SerialNo,
            archive.ProductName,
            archive.CustomerName
        }));
    }
}
