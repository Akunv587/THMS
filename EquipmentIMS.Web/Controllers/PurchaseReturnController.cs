using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("pur:return:view")]
public class PurchaseReturnController : Controller
{
    private readonly IPurchaseService _service;
    private readonly ICurrentUserService _user;

    public PurchaseReturnController(IPurchaseService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(PurchaseReturnQueryViewModel query, int page = 1, int pageSize = 20)
    {
        var dto = new PurchaseReturnQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Keyword = query.Keyword,
            SupplierId = query.SupplierId,
            WarehouseId = query.WarehouseId,
            StartDate = query.StartDate,
            EndDate = query.EndDate
        };
        var result = await _service.GetReturnListAsync(dto);
        ViewData["Query"] = query;
        return View(result);
    }

    [PermissionFilter("pur:return:create")]
    public IActionResult Create()
    {
        return View(new PurchaseReturnViewModel { ReturnDate = DateTime.Now });
    }

    [HttpPost]
    [PermissionFilter("pur:return:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseReturnViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (vm.Details.Count == 0)
        {
            ModelState.AddModelError("", "请至少选择一条退货明细");
            return View(vm);
        }

        var dto = new PurchaseReturnRequestDto
        {
            PurchaseInId = vm.PurchaseInId,
            ReturnDate = vm.ReturnDate,
            Reason = vm.Reason,
            Remark = vm.Remark,
            Details = vm.Details.Select(d => new PurchaseReturnItemRequestDto
            {
                ProductId = d.ProductId,
                ProductCode = d.ProductCode,
                ProductName = d.ProductName,
                Spec = d.Spec,
                Unit = d.Unit,
                InQty = d.InQty,
                ReturnedQty = d.ReturnedQty,
                RemainingQty = d.RemainingQty,
                Qty = d.ReturnQty,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount
            }).ToList()
        };

        try
        {
            await _service.PurchaseReturnAsync(dto, _user.RealName);
            return Json(ApiResponse.Ok("退货成功"));
        }
        catch (BusinessException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, ex.ErrorCode ?? "BIZ_ERROR"));
        }
    }
}
