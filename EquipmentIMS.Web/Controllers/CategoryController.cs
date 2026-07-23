using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("base:category:view")]
public class CategoryController : Controller
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var tree = await _service.GetTreeAsync();
        return View(tree);
    }

    [HttpGet]
    public async Task<IActionResult> GetTree()
    {
        var tree = await _service.GetTreeAsync();
        return Json(ApiResponse.Ok(tree));
    }

    [PermissionFilter("base:category:create")]
    public async Task<IActionResult> Create(long? parentId)
    {
        var tree = await _service.GetTreeAsync();
        ViewBag.Categories = tree;
        return View(new CategoryViewModel { ParentId = parentId });
    }

    [HttpPost]
    [PermissionFilter("base:category:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new CategoryDto { ParentId = vm.ParentId, Code = vm.Code, Name = vm.Name, SortOrder = vm.SortOrder };
        await _service.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("base:category:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        return View(new CategoryViewModel { Id = dto.Id, ParentId = dto.ParentId, Code = dto.Code, Name = dto.Name, SortOrder = dto.SortOrder });
    }

    [HttpPost]
    [PermissionFilter("base:category:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code, vm.Id))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new CategoryDto { Id = vm.Id, Code = vm.Code, Name = vm.Name, SortOrder = vm.SortOrder };
        await _service.UpdateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionFilter("base:category:delete")]
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
}
