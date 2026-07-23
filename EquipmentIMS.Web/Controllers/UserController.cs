using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[Route("[controller]")]
public class UserController : Controller
{
    private readonly IUserService _service;

    public UserController(IUserService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(UserQueryDto query)
    {
        var result = await _service.GetListAsync(query);
        ViewData["Keyword"] = query.Keyword;
        ViewData["Page"] = result.Page;
        ViewData["PageSize"] = result.PageSize;
        ViewData["TotalCount"] = result.TotalCount;
        return View(result);
    }

    [HttpGet("Create")]
    public IActionResult Create() => View(new UserCreateEditDto { IsActive = true });

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateEditDto dto)
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
        var user = await _service.GetByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserCreateEditDto dto)
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
        await _service.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ResetPassword/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(long id)
    {
        await _service.ResetPasswordAsync(id, "123456");
        TempData["Message"] = "密码已重置为 123456";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleActive/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        await _service.ToggleActiveAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("AssignRoles/{id}")]
    public async Task<IActionResult> AssignRoles(long id)
    {
        var dto = await _service.GetAssignRolesAsync(id);
        return View(dto);
    }

    [HttpPost("AssignRoles/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoles(long id, List<long> roleIds)
    {
        await _service.AssignRolesAsync(id, roleIds);
        TempData["Message"] = "角色分配成功";
        return RedirectToAction(nameof(Index));
    }
}
