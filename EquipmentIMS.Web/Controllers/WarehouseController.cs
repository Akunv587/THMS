using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("base:warehouse:view")]
public class WarehouseController : Controller
{
    private readonly IWarehouseService _service;

    public WarehouseController(IWarehouseService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? keyword, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetPagedAsync(page, pageSize, keyword);
        ViewData["Keyword"] = keyword;
        return View(result);
    }

    [PermissionFilter("base:warehouse:create")]
    public IActionResult Create()
    {
        return View(new WarehouseViewModel());
    }

    [HttpPost]
    [PermissionFilter("base:warehouse:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WarehouseViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new WarehouseDto
        {
            Code = vm.Code, Name = vm.Name, Address = vm.Address,
            Manager = vm.Manager, Phone = vm.Phone, IsDefault = vm.IsDefault
        };
        await _service.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("base:warehouse:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        return View(new WarehouseViewModel
        {
            Id = dto.Id, Code = dto.Code, Name = dto.Name, Address = dto.Address,
            Manager = dto.Manager, Phone = dto.Phone, IsDefault = dto.IsDefault
        });
    }

    [HttpPost]
    [PermissionFilter("base:warehouse:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WarehouseViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code, vm.Id))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new WarehouseDto
        {
            Id = vm.Id, Code = vm.Code, Name = vm.Name, Address = vm.Address,
            Manager = vm.Manager, Phone = vm.Phone, IsDefault = vm.IsDefault
        };
        await _service.UpdateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionFilter("base:warehouse:delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Json(ApiResponse.Ok("删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Json(ApiResponse.Fail(ex.Message, "DELETE_ERROR"));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWarehousesJson(string? keyword)
    {
        var list = await _service.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(keyword))
            list = list.Where(w => w.Code.Contains(keyword) || w.Name.Contains(keyword)).ToList();
        return Json(ApiResponse.Ok(list.Select(w => new { w.Id, w.Code, w.Name })));
    }
}
