using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("base:customer:view")]
public class CustomerController : Controller
{
    private readonly ICustomerService _service;
    private readonly IProductService _productService;
    private readonly ICurrentUserService _user;

    public CustomerController(ICustomerService service, IProductService productService, ICurrentUserService user)
    {
        _service = service;
        _productService = productService;
        _user = user;
    }

    public async Task<IActionResult> Index(string? keyword, int page = 1, int pageSize = 20)
    {
        var isManager = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        var result = await _service.GetPagedAsync(page, pageSize, _user.UserId, isManager, keyword);
        ViewData["Keyword"] = keyword;
        return View(result);
    }

    [PermissionFilter("base:customer:create")]
    public IActionResult Create()
    {
        return View(new CustomerViewModel());
    }

    [HttpPost]
    [PermissionFilter("base:customer:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new CustomerDto
        {
            Code = vm.Code, Name = vm.Name, ContactPerson = vm.ContactPerson,
            ContactPhone = vm.ContactPhone, Email = vm.Email, Address = vm.Address,
            CreditLimit = vm.CreditLimit, Remark = vm.Remark
        };
        await _service.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("base:customer:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        return View(new CustomerViewModel
        {
            Id = dto.Id, Code = dto.Code, Name = dto.Name, ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone, Email = dto.Email, Address = dto.Address,
            CreditLimit = dto.CreditLimit, Remark = dto.Remark
        });
    }

    [HttpPost]
    [PermissionFilter("base:customer:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.CodeExistsAsync(vm.Code, vm.Id))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            return View(vm);
        }
        var dto = new CustomerDto
        {
            Id = vm.Id, Code = vm.Code, Name = vm.Name, ContactPerson = vm.ContactPerson,
            ContactPhone = vm.ContactPhone, Email = vm.Email, Address = vm.Address,
            CreditLimit = vm.CreditLimit, Remark = vm.Remark
        };
        await _service.UpdateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionFilter("base:customer:delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomersJson(string? keyword)
    {
        var isManager = _user.IsInRole("Manager") || _user.IsInRole("Admin");
        var list = await _service.GetAllAsync(_user.UserId, isManager, keyword);
        return Json(ApiResponse.Ok(list.Select(c => new { c.Id, c.Code, c.Name })));
    }

    [PermissionFilter("base:customer:price")]
    public async Task<IActionResult> Price(long id)
    {
        var customer = await _service.GetByIdAsync(id);
        if (customer == null) return NotFound();
        ViewBag.CustomerId = id;
        ViewBag.CustomerName = customer.Name;
        var prices = await _service.GetPricesByCustomerAsync(id);
        return View(prices);
    }

    [HttpPost]
    [PermissionFilter("base:customer:price")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrice([FromBody] CustomerPriceDto dto)
    {
        await _service.SetPriceAsync(dto);
        return Json(ApiResponse.Ok("设置成功"));
    }

    [HttpPost]
    [PermissionFilter("base:customer:price")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice([FromBody] CustomerPriceDto dto)
    {
        await _service.UpdatePriceAsync(dto);
        return Json(ApiResponse.Ok("更新成功"));
    }

    [HttpPost]
    [PermissionFilter("base:customer:price")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePrice(long id)
    {
        await _service.DeletePriceAsync(id);
        return Json(ApiResponse.Ok("删除成功"));
    }
}
