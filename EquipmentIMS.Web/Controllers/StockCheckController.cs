using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("stock:check:view")]
public class StockCheckController : Controller
{
    private readonly IStockService _service;
    private readonly ICurrentUserService _user;

    public StockCheckController(IStockService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(int? status, string? keyword, int page = 1, int pageSize = 20)
    {
        var query = new StockCheckQueryDto
        {
            Status = status,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize
        };
        var result = await _service.GetCheckListAsync(query);

        ViewData["Status"] = status;
        ViewData["Keyword"] = keyword;
        ViewData["StatusList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "", Text = "全部状态" },
            new() { Value = "0", Text = "新建" },
            new() { Value = "1", Text = "盘点中" },
            new() { Value = "2", Text = "待审批" },
            new() { Value = "3", Text = "已审批" },
            new() { Value = "4", Text = "已取消" }
        }, "Value", "Text", status?.ToString());

        return View(result);
    }

    [PermissionFilter("stock:check:create")]
    public async Task<IActionResult> Create()
    {
        var warehouses = await _service.GetWarehousesAsync();
        ViewData["WarehouseList"] = new SelectList(warehouses, "Id", "Name");
        return View(new StockCheckCreateDto());
    }

    [HttpPost]
    [PermissionFilter("stock:check:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockCheckCreateDto dto)
    {
        if (dto.WarehouseId <= 0)
        {
            ModelState.AddModelError("WarehouseId", "请选择仓库");
            var warehouses = await _service.GetWarehousesAsync();
            ViewData["WarehouseList"] = new SelectList(warehouses, "Id", "Name");
            return View(dto);
        }

        try
        {
            var id = await _service.CreateCheckAsync(dto, _user.RealName);
            TempData["Success"] = "盘点单创建成功，系统已生成库存快照";
            return RedirectToAction(nameof(Record), new { id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var warehouses = await _service.GetWarehousesAsync();
            ViewData["WarehouseList"] = new SelectList(warehouses, "Id", "Name");
            return View(dto);
        }
    }

    [PermissionFilter("stock:check:record")]
    public async Task<IActionResult> Record(long id)
    {
        try
        {
            var detail = await _service.GetCheckDetailAsync(id);

            if (detail.Status == 0 || detail.Status == 1)
                return View(detail);

            // 非编辑状态跳转到详情
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [PermissionFilter("stock:check:record")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(long id, List<StockCheckRecordDto> records)
    {
        try
        {
            await _service.RecordActualAsync(id, records, _user.RealName);
            TempData["Success"] = "实盘数量已保存";
            return RedirectToAction(nameof(Record), new { id });
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Record), new { id });
        }
    }

    [HttpPost]
    [PermissionFilter("stock:check:submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(long id)
    {
        try
        {
            await _service.SubmitCheckAsync(id, _user.RealName);
            TempData["Success"] = "盘点单已提交审批";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [PermissionFilter("stock:check:detail")]
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var detail = await _service.GetCheckDetailAsync(id);
            ViewData["IsManager"] = _user.IsInRole("Manager") || _user.IsInRole("Admin");
            return View(detail);
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [PermissionFilter("stock:check:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id)
    {
        if (!_user.IsInRole("Manager") && !_user.IsInRole("Admin"))
        {
            TempData["Error"] = "仅经理角色可审批盘点";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _service.AdjustStockCheckAsync(id, _user.RealName);
            TempData["Success"] = "盘点审批通过，库存已调整";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("stock:check:cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id)
    {
        try
        {
            await _service.CancelCheckAsync(id, _user.RealName);
            TempData["Success"] = "盘点单已取消";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
