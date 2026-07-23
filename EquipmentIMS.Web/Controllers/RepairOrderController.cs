using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("maint:repair:view")]
public class RepairOrderController : Controller
{
    private readonly IMaintenanceService _service;
    private readonly IDeviceArchiveService _archiveService;
    private readonly IProductService _productService;
    private readonly IWarehouseService _warehouseService;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;

    public RepairOrderController(
        IMaintenanceService service,
        IDeviceArchiveService archiveService,
        IProductService productService,
        IWarehouseService warehouseService,
        ICurrentUserService user,
        IUnitOfWork uow)
    {
        _service = service;
        _archiveService = archiveService;
        _productService = productService;
        _warehouseService = warehouseService;
        _user = user;
        _uow = uow;
    }

    public async Task<IActionResult> Index(int? status, long? technicianId, int page = 1, int pageSize = 20)
    {
        var isManager = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        var result = await _service.GetListAsync(technicianId, status, null, _user.UserId, isManager, page, pageSize);
        ViewData["Status"] = status;
        ViewData["TechnicianId"] = technicianId;

        ViewData["StatusList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "", Text = "全部" },
            new() { Value = "0", Text = "待派工" },
            new() { Value = "1", Text = "已派工" },
            new() { Value = "2", Text = "维修中" },
            new() { Value = "3", Text = "待确认" },
            new() { Value = "4", Text = "已完工" },
            new() { Value = "5", Text = "已关闭" },
            new() { Value = "6", Text = "已取消" }
        }, "Value", "Text");

        if (isManager)
        {
            var users = await _uow.GetRepository<SysUser>().Query()
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.RealName)
                .ToListAsync();
            var techs = users.Select(u => new { u.Id, u.RealName }).ToList();
            ViewData["TechnicianList"] = new SelectList(techs, "Id", "RealName");
        }

        return View(result);
    }

    [PermissionFilter("maint:repair:create")]
    public IActionResult Create()
    {
        return View(new RepairOrderViewModel());
    }

    [HttpPost]
    [PermissionFilter("maint:repair:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RepairOrderViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var dto = new RepairOrderDto
        {
            DeviceArchiveId = vm.DeviceArchiveId,
            FaultDesc = vm.FaultDesc,
            FaultType = vm.FaultType,
            Remark = vm.Remark
        };
        var id = await _service.CreateAsync(dto, _user.RealName);
        return RedirectToAction(nameof(Details), new { id });
    }

    [PermissionFilter("maint:repair:view")]
    public async Task<IActionResult> Details(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();

        dto.Parts = await _service.GetRepairPartsAsync(id);

        var archive = await _archiveService.GetByIdAsync(dto.DeviceArchiveId);
        ViewData["Archive"] = archive;

        var isManager = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        var canDo = isManager || (_user.UserId == dto.TechnicianId);
        ViewData["CanOperate"] = canDo;

        if (isManager && dto.Status <= 1)
        {
            var users = await _uow.GetRepository<SysUser>().Query()
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.RealName)
                .ToListAsync();
            var techs = users.Select(u => new { u.Id, u.RealName }).ToList();
            ViewData["TechnicianList"] = new SelectList(techs, "Id", "RealName");
        }

        return View(dto);
    }

    [PermissionFilter("maint:repair:view")]
    public async Task<IActionResult> Dispatch(long id)
    {
        var order = await _service.GetByIdAsync(id);
        if (order == null || order.Status != 0) return RedirectToAction(nameof(Details), new { id });

        var users = await _uow.GetRepository<SysUser>().Query()
            .Where(u => u.IsActive && !u.IsDeleted)
            .OrderBy(u => u.RealName)
            .ToListAsync();
        var techs = users.Select(u => new { u.Id, u.RealName }).ToList();
        ViewData["TechnicianList"] = new SelectList(techs, "Id", "RealName");

        return View(new DispatchViewModel { OrderId = id, OrderNo = order.OrderNo });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:dispatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispatch(DispatchViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var users = await _uow.GetRepository<SysUser>().Query()
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.RealName)
                .ToListAsync();
            var techs = users.Select(u => new { u.Id, u.RealName }).ToList();
            ViewData["TechnicianList"] = new SelectList(techs, "Id", "RealName");
            return View(vm);
        }
        await _service.DispatchAsync(vm.OrderId, vm.TechnicianId, _user.RealName);
        return RedirectToAction(nameof(Details), new { id = vm.OrderId });
    }

    [PermissionFilter("maint:repair:view")]
    public async Task<IActionResult> Process(long id)
    {
        var order = await _service.GetByIdAsync(id);
        if (order == null || order.Status != 2) return RedirectToAction(nameof(Details), new { id });

        var parts = await _service.GetRepairPartsAsync(id);
        ViewData["Parts"] = parts;

        var products = await _productService.GetAllAsync();
        ViewData["ProductList"] = new SelectList(products, "Id", "Name");

        var warehouses = await _warehouseService.GetAllAsync();
        ViewData["WarehouseList"] = new SelectList(warehouses, "Id", "Name");

        return View(new ProcessViewModel { OrderId = id });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:process")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(ProcessViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.CompleteRepairAsync(vm.OrderId, vm.RepairDesc, vm.WorkHours, vm.RepairCost, _user.RealName);
        return RedirectToAction(nameof(Details), new { id = vm.OrderId });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:dispatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartRepair(long id)
    {
        await _service.StartRepairAsync(id, _user.RealName);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(long id)
    {
        await _service.ConfirmAsync(id, _user.RealName);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(long id)
    {
        await _service.CloseAsync(id, _user.RealName);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id)
    {
        await _service.CancelAsync(id, _user.RealName);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [PermissionFilter("maint:repair:process")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRepairPart(long orderId, long productId, long warehouseId, int qty)
    {
        if (qty <= 0)
            return Json(ApiResponse.Fail("数量必须大于0", "VAL_ERROR"));

        var order = await _service.GetByIdAsync(orderId);
        if (order == null)
            return Json(ApiResponse.Fail("工单不存在", "NOT_FOUND"));

        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
            return Json(ApiResponse.Fail("配件不存在", "NOT_FOUND"));

        var partsDto = new List<RepairPartDto>
        {
            new()
            {
                ProductId = productId,
                ProductName = product.Name,
                WarehouseId = warehouseId,
                Qty = qty,
                UnitPrice = product.PurchasePrice
            }
        };

        try
        {
            await _service.RecordRepairPartAsync(orderId, partsDto, _user.RealName);
            return Json(ApiResponse.Ok("领用成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "BIZ_ERROR"));
        }
    }

    [HttpGet]
    public async Task<IActionResult> SearchDeviceArchives(string term)
    {
        var archives = await _service.GetListAsync(null, null, null, _user.UserId, true, 1, 100);
        var filtered = string.IsNullOrWhiteSpace(term)
            ? archives.Items
            : archives.Items.Where(a => a.SerialNo != null && a.SerialNo.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        var result = filtered.Select(a => new { id = a.Id, text = $"{a.SerialNo} - {a.ProductName}（{a.CustomerName}）" });
        return Json(new { results = result });
    }

    [HttpGet]
    public async Task<IActionResult> GetArchiveInfo(long id)
    {
        var archive = await _archiveService.GetByIdAsync(id);
        if (archive == null) return Json(ApiResponse.Fail("设备档案不存在", "NOT_FOUND"));
        var isWarranty = archive.WarrantyEnd.HasValue && archive.WarrantyEnd.Value >= DateTime.Today;
        return Json(ApiResponse.Ok(new
        {
            archive.Id,
            archive.SerialNo,
            archive.ProductName,
            archive.CustomerId,
            archive.CustomerName,
            archive.WarrantyStart,
            archive.WarrantyEnd,
            isWarranty
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetStockInfo(long productId, long warehouseId)
    {
        var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(productId, warehouseId);
        return Json(ApiResponse.Ok(new
        {
            available = stock?.Quantity - stock?.LockQty ?? 0,
            quantity = stock?.Quantity ?? 0
        }));
    }
}
