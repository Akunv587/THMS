using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[Route("[controller]")]
public class RoleController : Controller
{
    private readonly IRoleService _service;

    public RoleController(IRoleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles = await _service.GetListAsync();
        return View(roles);
    }

    [HttpGet("Create")]
    public IActionResult Create() => View(new RoleCreateEditDto());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleCreateEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _service.CreateAsync(dto);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(long id)
    {
        var role = await _service.GetByIdAsync(id);
        if (role == null) return NotFound();
        return View(role);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleCreateEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _service.UpdateAsync(dto);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _service.DeleteAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("AssignPermissions/{id}")]
    public async Task<IActionResult> AssignPermissions(long id)
    {
        var dto = await _service.GetAssignPermissionsAsync(id);
        return View(dto);
    }

    [HttpPost("AssignPermissions/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermissions(long id, List<long> permissionIds)
    {
        await _service.AssignPermissionsAsync(id, permissionIds);
        TempData["Message"] = "权限分配成功";
        return RedirectToAction(nameof(Index));
    }
}
