using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("pur:order:view")]
public class PurchaseOrderController : Controller
{
    private readonly IPurchaseService _service;
    private readonly ICurrentUserService _user;

    public PurchaseOrderController(IPurchaseService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(PurchaseOrderQueryViewModel query, int page = 1, int pageSize = 20)
    {
        var dto = new PurchaseOrderQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Keyword = query.Keyword,
            SupplierId = query.SupplierId,
            Status = query.Status.HasValue ? (PurchaseOrderStatus)(byte)query.Status.Value : null,
            StartDate = query.StartDate,
            EndDate = query.EndDate
        };
        var result = await _service.GetListAsync(dto);
        ViewData["Query"] = query;
        return View(result);
    }

    [PermissionFilter("pur:order:create")]
    public IActionResult Create()
    {
        return View(new PurchaseOrderViewModel { OrderDate = DateTime.Now });
    }

    [HttpPost]
    [PermissionFilter("pur:order:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (vm.Details.Count == 0)
        {
            ModelState.AddModelError("", "至少添加一条设备明细");
            return View(vm);
        }

        var dto = new PurchaseOrderDto
        {
            SupplierId = vm.SupplierId,
            WarehouseId = vm.WarehouseId,
            OrderDate = vm.OrderDate,
            ExpectedDate = vm.ExpectedDate,
            Remark = vm.Remark,
            Details = vm.Details.Select(d => new PurchaseOrderDetailDto
            {
                ProductId = long.Parse(d.ProductId),
                Qty = d.Qty,
                UnitPrice = d.UnitPrice,
                Amount = d.Qty * d.UnitPrice,
                Remark = d.Remark
            }).ToList()
        };

        await _service.CreateAsync(dto, _user.RealName);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("pur:order:detail")]
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id);
            ViewData["IsManager"] = _user.IsInRole("manager");
            return View(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [PermissionFilter("pur:order:detail")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id);
            return Json(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [PermissionFilter("pur:order:submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(long id)
    {
        try
        {
            await _service.SubmitAsync(id, _user.RealName);
            return Json(ApiResponse.Ok("提交成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpPost]
    [PermissionFilter("pur:order:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id)
    {
        try
        {
            await _service.ApproveAsync(id, _user.UserId, _user.RealName);
            return Json(ApiResponse.Ok("审批通过"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpPost]
    [PermissionFilter("pur:order:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Json(ApiResponse.Fail("驳回原因不能为空", "VAL_ERROR"));

        try
        {
            await _service.RejectAsync(id, _user.UserId, _user.RealName, reason);
            return Json(ApiResponse.Ok("已驳回"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpPost]
    [PermissionFilter("pur:order:cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id)
    {
        try
        {
            await _service.CancelAsync(id, _user.RealName);
            return Json(ApiResponse.Ok("已取消"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }
}
