using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("base:supplier:view")]
public class SupplierController : Controller
{
    private readonly ISupplierService _service;

    public SupplierController(ISupplierService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? keyword, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetPagedAsync(page, pageSize, keyword);
        ViewData["Keyword"] = keyword;
        return View(result);
    }

    [PermissionFilter("base:supplier:view")]
    public async Task<IActionResult> Details(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    [PermissionFilter("base:supplier:create")]
    public IActionResult Create()
    {
        return View(new SupplierViewModel());
    }

    [HttpPost]
    [PermissionFilter("base:supplier:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new SupplierDto
        {
            Code = vm.Code, Name = vm.Name, ContactPerson = vm.ContactPerson,
            ContactPhone = vm.ContactPhone, Email = vm.Email, Address = vm.Address,
            BankAccount = vm.BankAccount, BankName = vm.BankName, Remark = vm.Remark
        };
        await _service.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("base:supplier:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        return View(new SupplierViewModel
        {
            Id = dto.Id, Code = dto.Code, Name = dto.Name, ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone, Email = dto.Email, Address = dto.Address,
            BankAccount = dto.BankAccount, BankName = dto.BankName, Remark = dto.Remark
        });
    }

    [HttpPost]
    [PermissionFilter("base:supplier:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code, vm.Id))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new SupplierDto
        {
            Id = vm.Id, Code = vm.Code, Name = vm.Name, ContactPerson = vm.ContactPerson,
            ContactPhone = vm.ContactPhone, Email = vm.Email, Address = vm.Address,
            BankAccount = vm.BankAccount, BankName = vm.BankName, Remark = vm.Remark
        };
        await _service.UpdateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionFilter("base:supplier:delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliersJson(string? keyword)
    {
        var list = await _service.GetAllAsync(keyword);
        return Json(ApiResponse.Ok(list.Select(s => new { s.Id, s.Code, s.Name })));
    }
}
