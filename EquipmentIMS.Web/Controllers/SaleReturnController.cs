using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("sale:return:view")]
public class SaleReturnController : Controller
{
    private readonly ISaleService _service;
    private readonly ICurrentUserService _user;

    public SaleReturnController(ISaleService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(SaleReturnQueryViewModel query, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetReturnListAsync(page, pageSize,
            query.Keyword, query.StartDate, query.EndDate, query.Status);
        ViewData["Query"] = query;
        ViewData["IsManager"] = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        return View(result);
    }

    [PermissionFilter("sale:return:create")]
    public IActionResult Create()
    {
        return View(new SaleReturnViewModel
        {
            ReturnDate = DateTime.Now,
            Operator = _user.RealName
        });
    }

    [HttpPost]
    [PermissionFilter("sale:return:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleReturnViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var dto = new SaleReturnDto
        {
            SaleOutId = vm.SaleOutId,
            CustomerId = 0,
            WarehouseId = vm.Details.FirstOrDefault()?.WarehouseId ?? 0,
            ReturnDate = vm.ReturnDate,
            Reason = vm.Reason,
            Operator = vm.Operator,
            Remark = vm.Remark,
            Details = vm.Details
                .Where(d => d.ReturnQty > 0)
                .Select(d => new SaleReturnDetailDto
                {
                    SaleOutDetailId = d.SaleOutDetailId,
                    ProductId = d.ProductId,
                    ProductName = d.ProductName ?? "",
                    ProductCode = d.ProductCode ?? "",
                    WarehouseId = d.WarehouseId,
                    WarehouseName = d.WarehouseName,
                    ReturnQty = d.ReturnQty,
                    OutQty = d.OutQty,
                    UnitPrice = d.UnitPrice
                }).ToList()
        };

        try
        {
            var id = await _service.CreateReturnAsync(dto, _user.RealName);
            TempData["Success"] = "退货单创建成功";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    [PermissionFilter("sale:return:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id)
    {
        if (!_user.IsInRole("Manager") && !_user.IsInRole("Admin"))
            return Json(ApiResponse.Fail("仅经理可审批", "NO_PERMISSION"));

        try
        {
            await _service.ApproveReturnAsync(id, _user.RealName);
            return Json(ApiResponse.Ok("审批通过并已入库"));
        }
        catch (BusinessException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, ex.ErrorCode));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpPost]
    [PermissionFilter("sale:return:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id, string reason)
    {
        if (!_user.IsInRole("Manager") && !_user.IsInRole("Admin"))
            return Json(ApiResponse.Fail("仅经理可审批", "NO_PERMISSION"));

        if (string.IsNullOrWhiteSpace(reason))
            return Json(ApiResponse.Fail("驳回原因不能为空", "PARAM_ERROR"));

        try
        {
            await _service.RejectReturnAsync(id, _user.RealName, reason);
            return Json(ApiResponse.Ok("已驳回"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpGet]
    [PermissionFilter("sale:return:create")]
    public async Task<IActionResult> GetOutDetails(long saleOutId)
    {
        try
        {
            var details = await _service.GetOutDetailsForReturnAsync(saleOutId);
            return Json(ApiResponse.Ok(details));
        }
        catch (KeyNotFoundException)
        {
            return Json(ApiResponse.Fail("出库单不存在", "NOT_FOUND"));
        }
    }

    [HttpGet]
    [PermissionFilter("sale:return:create")]
    public async Task<IActionResult> GetShippedOuts(string? keyword)
    {
        var result = await _service.GetSaleOutListAsync(1, 50, keyword, null, null);
        var available = result.Items
            .Select(o => new { o.Id, o.OutNo, o.CustomerName, o.TotalAmount })
            .ToList();
        return Json(ApiResponse.Ok(available));
    }
}
