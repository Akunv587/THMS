using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("maint:archive:view")]
public class DeviceArchiveController : Controller
{
    private readonly IDeviceArchiveService _service;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly ICurrentUserService _user;

    public DeviceArchiveController(
        IDeviceArchiveService service,
        ICustomerService customerService,
        IProductService productService,
        ICurrentUserService user)
    {
        _service = service;
        _customerService = customerService;
        _productService = productService;
        _user = user;
    }

    public async Task<IActionResult> Index(string? serialNo, long? customerId, int? status, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetListAsync(serialNo, customerId, status, page, pageSize);
        ViewData["SerialNo"] = serialNo;
        ViewData["CustomerId"] = customerId;
        ViewData["Status"] = status;

        var customers = await _customerService.GetAllAsync(_user.UserId, _user.IsInRole("Manager"), null);
        ViewData["CustomerList"] = new SelectList(customers, "Id", "Name");

        ViewData["StatusList"] = new SelectList(new List<SelectListItem>
        {
            new() { Value = "", Text = "全部" },
            new() { Value = "1", Text = "正常" },
            new() { Value = "2", Text = "维修中" },
            new() { Value = "3", Text = "报废" }
        }, "Value", "Text");

        return View(result);
    }

    [PermissionFilter("maint:archive:create")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new DeviceArchiveViewModel());
    }

    [HttpPost]
    [PermissionFilter("maint:archive:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeviceArchiveViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(vm);
        }

        var existing = await _service.FindBySerialNoAsync(vm.SerialNo);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(vm.SerialNo), "序列号已存在");
            await LoadDropdowns();
            return View(vm);
        }

        var dto = new DeviceArchiveDto
        {
            SerialNo = vm.SerialNo,
            ProductId = vm.ProductId,
            CustomerId = vm.CustomerId,
            SaleOrderId = vm.SaleOrderId,
            InstallDate = vm.InstallDate,
            WarrantyStart = vm.WarrantyStart,
            WarrantyEnd = vm.WarrantyEnd,
            Remark = vm.Remark
        };
        await _service.CreateAsync(dto, _user.RealName);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("maint:archive:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();

        await LoadDropdowns();
        return View("Create", new DeviceArchiveViewModel
        {
            Id = dto.Id,
            SerialNo = dto.SerialNo,
            ProductId = dto.ProductId,
            CustomerId = dto.CustomerId,
            SaleOrderId = dto.SaleOrderId,
            InstallDate = dto.InstallDate,
            WarrantyStart = dto.WarrantyStart,
            WarrantyEnd = dto.WarrantyEnd,
            Remark = dto.Remark
        });
    }

    [HttpPost]
    [PermissionFilter("maint:archive:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DeviceArchiveViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View("Create", vm);
        }

        var dto = new DeviceArchiveDto
        {
            Id = vm.Id,
            SerialNo = vm.SerialNo,
            ProductId = vm.ProductId,
            CustomerId = vm.CustomerId,
            SaleOrderId = vm.SaleOrderId,
            InstallDate = vm.InstallDate,
            WarrantyStart = vm.WarrantyStart,
            WarrantyEnd = vm.WarrantyEnd,
            Remark = vm.Remark
        };
        await _service.UpdateAsync(dto, _user.RealName);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("maint:archive:view")]
    public async Task<IActionResult> Details(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();

        var repairOrders = await _service.GetRepairOrdersAsync(id);
        ViewData["RepairOrders"] = repairOrders;
        return View(dto);
    }

    [HttpPost]
    [PermissionFilter("maint:archive:delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> FindBySerialNo(string serialNo)
    {
        if (string.IsNullOrWhiteSpace(serialNo))
            return Json(ApiResponse.Fail("序列号不能为空", "VAL_ERROR"));

        var dto = await _service.FindBySerialNoAsync(serialNo.Trim());
        return dto == null
            ? Json(ApiResponse.Fail("未找到该设备", "NOT_FOUND"))
            : Json(ApiResponse.Ok(dto));
    }

    private async Task LoadDropdowns()
    {
        var customers = await _customerService.GetAllAsync(_user.UserId, _user.IsInRole("Manager"), null);
        ViewData["CustomerList"] = new SelectList(customers, "Id", "Name");

        var products = await _productService.GetAllAsync(null);
        ViewData["ProductList"] = new SelectList(products, "Id", "Name");
    }
}
