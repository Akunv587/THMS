using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

[PermissionFilter("base:product:view")]
public class ProductController : Controller
{
    private readonly IProductService _service;
    private readonly ICategoryService _categoryService;

    public ProductController(IProductService service, ICategoryService categoryService)
    {
        _service = service;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(string? keyword, long? categoryId, int page = 1, int pageSize = 20)
    {
        var result = await _service.GetPagedAsync(page, pageSize, keyword, categoryId);
        ViewData["Keyword"] = keyword;
        ViewData["CategoryId"] = categoryId;
        ViewBag.Categories = await _categoryService.GetTreeAsync();
        return View(result);
    }

    [PermissionFilter("base:product:create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _categoryService.GetTreeAsync();
        return View(new ProductViewModel());
    }

    [HttpPost]
    [PermissionFilter("base:product:create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetTreeAsync();
            return View(vm);
        }
        if (await _service.CodeExistsAsync(vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            ViewBag.Categories = await _categoryService.GetTreeAsync();
            return View(vm);
        }
        var dto = new ProductDto
        {
            Code = vm.Code, Name = vm.Name, CategoryId = vm.CategoryId,
            Spec = vm.Spec, Brand = vm.Brand, Unit = vm.Unit, BarCode = vm.BarCode,
            PurchasePrice = vm.PurchasePrice, SalePrice = vm.SalePrice,
            MinStock = vm.MinStock, Remark = vm.Remark
        };
        await _service.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [PermissionFilter("base:product:edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null) return NotFound();
        ViewBag.Categories = await _categoryService.GetTreeAsync();
        return View(new ProductViewModel
        {
            Id = dto.Id, Code = dto.Code, Name = dto.Name, CategoryId = dto.CategoryId,
            Spec = dto.Spec, Brand = dto.Brand, Unit = dto.Unit, BarCode = dto.BarCode,
            PurchasePrice = dto.PurchasePrice, SalePrice = dto.SalePrice,
            MinStock = dto.MinStock, Remark = dto.Remark
        });
    }

    [HttpPost]
    [PermissionFilter("base:product:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetTreeAsync();
            return View(vm);
        }
        if (await _service.CodeExistsAsync(vm.Code, vm.Id))
        {
            ModelState.AddModelError(nameof(vm.Code), "编码已存在");
            ViewBag.Categories = await _categoryService.GetTreeAsync();
            return View(vm);
        }
        var dto = new ProductDto
        {
            Id = vm.Id, Code = vm.Code, Name = vm.Name, CategoryId = vm.CategoryId,
            Spec = vm.Spec, Brand = vm.Brand, Unit = vm.Unit, BarCode = vm.BarCode,
            PurchasePrice = vm.PurchasePrice, SalePrice = vm.SalePrice,
            MinStock = vm.MinStock, Remark = vm.Remark
        };
        await _service.UpdateAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionFilter("base:product:delete")]
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

    [PermissionFilter("base:product:import")]
    public IActionResult Import()
    {
        return View(new ProductImportViewModel());
    }

    [HttpGet]
    [PermissionFilter("base:product:import")]
    public IActionResult DownloadTemplate()
    {
        var stream = new MemoryStream();
        var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = workbook.CreateSheet("设备信息");

        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("编码");
        header.CreateCell(1).SetCellValue("名称");
        header.CreateCell(2).SetCellValue("分类");
        header.CreateCell(3).SetCellValue("规格型号");
        header.CreateCell(4).SetCellValue("品牌");
        header.CreateCell(5).SetCellValue("单位");
        header.CreateCell(6).SetCellValue("条码");
        header.CreateCell(7).SetCellValue("采购价");
        header.CreateCell(8).SetCellValue("销售价");
        header.CreateCell(9).SetCellValue("最低库存");

        for (int i = 0; i < 10; i++)
            sheet.SetColumnWidth(i, 4000);

        workbook.Write(stream, leaveOpen: false);
        stream.Position = 0;

        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "设备导入模板.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> FindByBarCode(string barCode)
    {
        if (string.IsNullOrWhiteSpace(barCode))
            return Json(new { success = false, message = "条码不能为空" });

        var product = await _service.FindByBarCodeAsync(barCode);
        if (product == null)
            return Json(new { success = false, message = "未找到对应设备" });

        return Json(new { success = true, data = product });
    }

    [HttpGet]
    public async Task<IActionResult> GetProductsJson(string? keyword)
    {
        var list = await _service.GetAllAsync(keyword);
        return Json(ApiResponse.Ok(list.Select(p => new { p.Id, p.Code, p.Name, p.Spec })));
    }

    [HttpPost]
    [PermissionFilter("base:product:import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(ProductImportViewModel vm)
    {
        if (vm.File == null || vm.File.Length == 0)
        {
            ModelState.AddModelError(nameof(vm.File), "请选择Excel文件");
            return View(vm);
        }
        var result = await _service.ImportExcelAsync(vm.File);
        ViewBag.ImportResult = result;
        return View(vm);
    }
}
