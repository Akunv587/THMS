using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("stock:transfer:view")]
public class StockTransferController : Controller
{
    private readonly IStockService _service;
    private readonly ICurrentUserService _user;

    public StockTransferController(IStockService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(int? status, string? keyword, int page = 1, int pageSize = 20)
    {
        var query = new StockTransferQueryDto
        {
            Status = status,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize
        };
        var result = await _service.GetTransferListAsync(query);

        ViewData["Status"] = status;
        ViewData["Keyword"] = keyword;
        ViewData["StatusList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "", Text = "全部状态" },
            new() { Value = "0", Text = "草稿" },
            new() { Value = "1", Text = "待审批" },
            new() { Value = "2", Text = "已审批" },
            new() { Value = "3", Text = "已完成" },
            new() { Value = "4", Text = "已取消" },
            new() { Value = "5", Text = "已驳回" }
        }, "Value", "Text", status?.ToString());

        return View(result);
    }

    [PermissionFilter("stock:transfer:create")]
    public async Task<IActionResult> Create()
    {
        var warehouses = await _service.GetWarehousesAsync();
        ViewData["WarehouseList"] = new SelectList(warehouses, "Id", "Name");
        return View(new StockTransferCreateDto());
    }

    [HttpPost]
    [PermissionFilter("stock:transfer:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockTransferCreateDto dto)
    {
        try
        {
            var id = await _service.CreateTransferAsync(dto, _user.RealName);
            TempData["Success"] = "调拨单创建成功";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var warehouses = await _service.GetWarehousesAsync();
            ViewData["WarehouseList"] = new SelectList(warehouses, "Id", "Name");
            return View(dto);
        }
    }

    [PermissionFilter("stock:transfer:detail")]
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var detail = await _service.GetTransferDetailAsync(id);
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
    [PermissionFilter("stock:transfer:submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(long id)
    {
        try
        {
            await _service.SubmitTransferAsync(id, _user.RealName);
            TempData["Success"] = "调拨单已提交审批";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("stock:transfer:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id)
    {
        if (!_user.IsInRole("Manager") && !_user.IsInRole("Admin"))
        {
            TempData["Error"] = "仅经理角色可审批调拨";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _service.ApproveTransferAsync(id, _user.RealName);
            TempData["Success"] = "调拨单审批通过";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("stock:transfer:reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id, string reason)
    {
        if (!_user.IsInRole("Manager") && !_user.IsInRole("Admin"))
        {
            TempData["Error"] = "仅经理角色可驳回调拨";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _service.RejectTransferAsync(id, _user.RealName, reason);
            TempData["Success"] = "调拨单已驳回";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("stock:transfer:execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(long id)
    {
        try
        {
            await _service.ExecuteTransferAsync(id, _user.RealName);
            TempData["Success"] = "调拨执行成功，库存已更新";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("stock:transfer:cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id)
    {
        try
        {
            await _service.CancelTransferAsync(id, _user.RealName);
            TempData["Success"] = "调拨单已取消";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>获取调出仓库可调拨设备列表（AJAX）</summary>
    [PermissionFilter("stock:transfer:create")]
    public async Task<IActionResult> GetProducts(long warehouseId)
    {
        if (warehouseId <= 0)
            return Json(new { success = false, message = "请选择仓库" });

        var products = await _service.GetTransferProductsAsync(warehouseId);
        return Json(new { success = true, data = products });
    }
}
