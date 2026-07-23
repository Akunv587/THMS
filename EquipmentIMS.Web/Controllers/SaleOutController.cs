using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("sale:out:view")]
public class SaleOutController : Controller
{
    private readonly ISaleService _service;
    private readonly ICurrentUserService _user;
    private readonly ISystemConfigService _config;

    public SaleOutController(ISaleService service, ICurrentUserService user, ISystemConfigService config)
    {
        _service = service;
        _user = user;
        _config = config;
    }

    public async Task<IActionResult> Index(SaleOutQueryViewModel query, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetSaleOutListAsync(page, pageSize,
            query.Keyword, query.StartDate, query.EndDate);
        ViewData["Query"] = query;
        return View(result);
    }

    [PermissionFilter("sale:out:create")]
    public IActionResult Create(long? saleOrderId)
    {
        return View(new SaleOutViewModel
        {
            SaleOrderId = saleOrderId ?? 0,
            OutTime = DateTime.Now,
            Operator = _user.RealName
        });
    }

    [HttpPost]
    [PermissionFilter("sale:out:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleOutViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var dto = new SaleOutDto
        {
            SaleOrderId = vm.SaleOrderId,
            OutTime = vm.OutTime,
            Operator = vm.Operator,
            Remark = vm.Remark,
            Details = vm.Details
                .Where(d => d.OutQty > 0)
                .Select(d => new SaleOutDetailDto
                {
                    SaleOrderDetailId = d.SaleOrderDetailId,
                    ProductId = d.ProductId,
                    WarehouseId = d.WarehouseId,
                    Qty = d.OutQty,
                    UnitPrice = d.UnitPrice
                }).ToList()
        };

        try
        {
            var result = await _service.SaleOutAsync(dto, _user.RealName);
            return RedirectToAction(nameof(Details), new { id = result.Id });
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

    [PermissionFilter("sale:out:detail")]
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var dto = await _service.GetSaleOutByIdAsync(id);
            return View(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [PermissionFilter("sale:out:create")]
    public async Task<IActionResult> GetOrderDetails(long saleOrderId)
    {
        try
        {
            var details = await _service.GetOrderDetailsForOutAsync(saleOrderId);
            return Json(ApiResponse.Ok(details));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
        catch (KeyNotFoundException)
        {
            return Json(ApiResponse.Fail("订单不存在", "NOT_FOUND"));
        }
    }

    [HttpGet]
    [PermissionFilter("sale:out:detail")]
    public async Task<IActionResult> GetApprovedOrders(string? keyword)
    {
        var isManager = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        var query = new SaleOrderQueryDto
        {
            Page = 1,
            PageSize = 50,
            Keyword = keyword
        };
        var result = await _service.GetListAsync(query, _user.RealName, isManager);

        // 只返回已审批和部分出库状态的订单
        var availableOrders = result.Items
            .Where(o => o.Status == SaleOrderStatus.Approved || o.Status == SaleOrderStatus.PartialShipped)
            .Select(o => new { o.Id, o.OrderNo, o.CustomerName, o.TotalAmount, o.ShippedAmount })
            .ToList();

        return Json(ApiResponse.Ok(availableOrders));
    }

    [PermissionFilter("sale:out:detail")]
    public async Task<IActionResult> Print(long id)
    {
        try
        {
            var dto = await _service.GetSaleOutByIdAsync(id);
            ViewData["CompanyName"] = await _config.GetAsync("CompanyName") ?? "郑州恒锐自动化设备有限公司";
            ViewData["CompanyAddress"] = await _config.GetAsync("CompanyAddress") ?? "";
            ViewData["CompanyPhone"] = await _config.GetAsync("CompanyPhone") ?? "";
            return View(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
