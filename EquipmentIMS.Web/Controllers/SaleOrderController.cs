using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("sale:order:view")]
public class SaleOrderController : Controller
{
    private readonly ISaleService _service;
    private readonly ICurrentUserService _user;
    private readonly ICustomerService _customerService;

    public SaleOrderController(ISaleService service, ICurrentUserService user, ICustomerService customerService)
    {
        _service = service;
        _user = user;
        _customerService = customerService;
    }

    public async Task<IActionResult> Index(SaleOrderQueryViewModel query, int page = 1, int pageSize = 20)
    {
        var dto = new SaleOrderQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Keyword = query.Keyword,
            CustomerId = query.CustomerId,
            Status = query.Status.HasValue ? (SaleOrderStatus)(byte)query.Status.Value : null,
            StartDate = query.StartDate,
            EndDate = query.EndDate
        };
        var isManager = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        var result = await _service.GetListAsync(dto, _user.RealName, isManager);
        ViewData["Query"] = query;
        return View(result);
    }

    [PermissionFilter("sale:order:create")]
    public IActionResult Create()
    {
        return View(new SaleOrderViewModel { OrderDate = DateTime.Now });
    }

    [HttpPost]
    [PermissionFilter("sale:order:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleOrderViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (vm.Details.Count == 0)
        {
            ModelState.AddModelError("", "至少添加一条设备明细");
            return View(vm);
        }

        var dto = new SaleOrderDto
        {
            CustomerId = vm.CustomerId,
            OrderDate = vm.OrderDate,
            ExpectedDate = vm.ExpectedDate,
            Remark = vm.Remark,
            Details = vm.Details.Select(d => new SaleOrderDetailDto
            {
                ProductId = long.Parse(d.ProductId),
                WarehouseId = long.Parse(d.WarehouseId),
                Qty = d.Qty,
                UnitPrice = d.UnitPrice,
                Amount = d.Qty * d.UnitPrice,
                Remark = d.Remark
            }).ToList()
        };

        try
        {
            await _service.CreateAsync(dto, _user.RealName);
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    [PermissionFilter("sale:order:detail")]
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id);
            ViewData["IsManager"] = _user.IsInRole("admin") || _user.IsInRole("manager");
            return View(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [PermissionFilter("sale:order:submit")]
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
        catch (BusinessException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, ex.ErrorCode));
        }
    }

    [HttpPost]
    [PermissionFilter("sale:order:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id)
    {
        try
        {
            await _service.ApproveAsync(id, _user.RealName);
            return Json(ApiResponse.Ok("审批通过"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
        catch (BusinessException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, ex.ErrorCode));
        }
    }

    [HttpPost]
    [PermissionFilter("sale:order:approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Json(ApiResponse.Fail("驳回原因不能为空", "VAL_ERROR"));

        try
        {
            await _service.RejectAsync(id, _user.RealName, reason);
            return Json(ApiResponse.Ok("已驳回"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpPost]
    [PermissionFilter("sale:order:cancel")]
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

    [HttpGet]
    [PermissionFilter("sale:order:detail")]
    public async Task<IActionResult> GetCustomerPrice(long customerId, long productId)
    {
        var price = await _service.GetCustomerPriceAsync(customerId, productId);
        return Json(ApiResponse.Ok(price));
    }

    [HttpGet]
    [PermissionFilter("sale:order:detail")]
    public async Task<IActionResult> GetCustomerCredit(long customerId)
    {
        var customer = await _customerService.GetByIdAsync(customerId);
        if (customer == null)
            return Json(ApiResponse.Fail("客户不存在", "NOT_FOUND"));
        return Json(ApiResponse.Ok(new
        {
            customer.CreditLimit,
            customer.UsedCredit,
            Remaining = customer.CreditLimit - customer.UsedCredit
        }));
    }
}
