using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("pur:in:view")]
public class PurchaseInController : Controller
{
    private readonly IPurchaseService _service;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;

    public PurchaseInController(IPurchaseService service, ICurrentUserService user, IUnitOfWork uow)
    {
        _service = service;
        _user = user;
        _uow = uow;
    }

    public async Task<IActionResult> Index(PurchaseInQueryViewModel query, int page = 1, int pageSize = 20)
    {
        var dto = new PurchaseInQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Keyword = query.Keyword,
            SupplierId = query.SupplierId,
            WarehouseId = query.WarehouseId,
            StartDate = query.StartDate,
            EndDate = query.EndDate
        };
        var result = await _service.GetInListAsync(dto);
        ViewData["Query"] = query;
        return View(result);
    }

    [PermissionFilter("pur:in:create")]
    public IActionResult Create()
    {
        return View(new PurchaseInViewModel { InDate = DateTime.Now });
    }

    [HttpPost]
    [PermissionFilter("pur:in:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseInViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (vm.Details.Count == 0)
        {
            ModelState.AddModelError("", "请至少选择一条入库明细");
            return View(vm);
        }

        var dto = new PurchaseInRequestDto
        {
            PurchaseOrderId = vm.PurchaseOrderId,
            InDate = vm.InDate,
            Remark = vm.Remark,
            Details = vm.Details.Select(d => new PurchaseInItemRequestDto
            {
                ProductId = d.ProductId,
                ProductCode = d.ProductCode,
                ProductName = d.ProductName,
                Spec = d.Spec,
                Unit = d.Unit,
                OrderQty = d.OrderQty,
                ReceivedQty = d.ReceivedQty,
                RemainingQty = d.RemainingQty,
                Qty = d.InQty,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount
            }).ToList()
        };

        try
        {
            await _service.PurchaseInAsync(dto, _user.RealName);
            return Json(ApiResponse.Ok("入库成功"));
        }
        catch (BusinessException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, ex.ErrorCode ?? "BIZ_ERROR"));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var q = await _uow.GetRepository<PurPurchaseIn>().Query()
            .Include(i => i.Details).ThenInclude(d => d.Product)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (q == null) return NotFound();
        return Json(new
        {
            q.Id,
            q.InNo,
            q.PurchaseOrderId,
            OrderNo = q.PurchaseOrder?.OrderNo ?? "",
            q.SupplierId,
            SupplierName = q.Supplier?.Name ?? "",
            q.WarehouseId,
            WarehouseName = q.Warehouse?.Name ?? "",
            q.InDate,
            q.TotalAmount,
            q.Status,
            q.Operator,
            q.Remark,
            Details = q.Details.Select(d => new
            {
                d.Id,
                d.ProductId,
                ProductCode = d.Product?.Code ?? "",
                ProductName = d.Product?.Name ?? "",
                Spec = d.Product?.Spec,
                Unit = d.Product?.Unit,
                d.Qty,
                d.UnitPrice,
                d.Amount,
                d.ReturnedQty
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetApprovedOrdersForIn()
    {
        var result = await _service.GetListAsync(new PurchaseOrderQueryDto
        {
            Page = 1,
            PageSize = 200,
            Status = null // 返回所有非草稿订单，前端按状态过滤
        });
        // 只返回已审批或部分入库的订单
        var approvedOrders = result.Items
            .Where(o => o.Status == EquipmentIMS.Models.Enums.PurchaseOrderStatus.Approved
                     || o.Status == EquipmentIMS.Models.Enums.PurchaseOrderStatus.PartialReceived)
            .Select(o => new { o.Id, o.OrderNo, o.SupplierName, o.TotalAmount, Status = (int)o.Status });
        return Json(ApiResponse.Ok(approvedOrders));
    }
}
