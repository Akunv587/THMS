using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[Route("[controller]")]
[PermissionFilter("stock:view")]
public class StockController : Controller
{
    private readonly IStockService _service;
    private readonly ICurrentUserService _user;

    public StockController(IStockService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? keyword = null,
        long? categoryId = null, long? warehouseId = null, bool? warningOnly = null)
    {
        var query = new StockQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword,
            CategoryId = categoryId,
            WarehouseId = warehouseId,
            WarningOnly = warningOnly
        };

        var result = await _service.GetStockListAsync(query);

        ViewData["Categories"] = await _service.GetCategoriesAsync();
        ViewData["Warehouses"] = await _service.GetWarehousesAsync();
        ViewData["Page"] = result.Page;
        ViewData["PageSize"] = result.PageSize;
        ViewData["TotalCount"] = result.TotalCount;
        ViewData["ActionName"] = "Index";
        ViewData["ControllerName"] = "Stock";
        ViewData["RouteValues"] = new Dictionary<string, object?>
        {
            ["keyword"] = keyword,
            ["categoryId"] = categoryId,
            ["warehouseId"] = warehouseId,
            ["warningOnly"] = warningOnly
        };

        return View("Index", result.Items);
    }

    [HttpGet("Warning")]
    [PermissionFilter("stock:view")]
    public async Task<IActionResult> Warning()
    {
        var warnings = await _service.GetWarningsAsync();
        var stats = await _service.GetWarningStatsAsync();

        ViewData["Stats"] = stats;
        ViewData["Categories"] = await _service.GetCategoriesAsync();
        ViewData["Warehouses"] = await _service.GetWarehousesAsync();

        return View(warnings);
    }

    [HttpGet("WarningChartData")]
    public async Task<IActionResult> GetWarningChartData()
    {
        var stats = await _service.GetWarningStatsAsync();
        var categoryData = await _service.GetCategoryWarningsAsync();

        return Json(new
        {
            pie = new[]
            {
                new { name = "缺货", value = stats.OutOfStockCount, color = "#dc2626" },
                new { name = "低库存", value = stats.LowStockCount, color = "#f59e0b" },
                new { name = "正常", value = stats.NormalCount, color = "#16a34a" }
            },
            bar = categoryData.Select(c => new
            {
                name = c.CategoryName,
                outOfStock = c.OutOfStockCount,
                lowStock = c.LowStockCount,
                total = c.TotalCount
            })
        });
    }

    [HttpGet("ExportWarningToExcel")]
    [PermissionFilter("stock:view")]
    public async Task<IActionResult> ExportWarningToExcel(string? keyword, string? level, long? warehouseId, long? categoryId)
    {
        var warnings = await _service.GetWarningsAsync();

        // 应用筛选
        if (!string.IsNullOrEmpty(level))
        {
            warnings = warnings.Where(w => w.AlertLevelName == level).ToList();
        }
        if (!string.IsNullOrEmpty(keyword))
        {
            var kw = keyword.ToLower();
            warnings = warnings.Where(w =>
                w.ProductCode.ToLower().Contains(kw) ||
                w.ProductName.ToLower().Contains(kw) ||
                w.WarehouseName.ToLower().Contains(kw)).ToList();
        }
        if (warehouseId.HasValue)
            warnings = warnings.Where(w => w.WarehouseId == warehouseId.Value).ToList();

        var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = workbook.CreateSheet("库存预警清单");

        var header = sheet.CreateRow(0);
        var headers = new[] { "设备编码", "设备名称", "规格型号", "仓库", "当前库存", "可用库存", "安全库存", "预警级别" };
        for (int i = 0; i < headers.Length; i++)
        {
            header.CreateCell(i).SetCellValue(headers[i]);
            sheet.SetColumnWidth(i, 4000);
        }

        for (int i = 0; i < warnings.Count; i++)
        {
            var row = sheet.CreateRow(i + 1);
            var w = warnings[i];
            row.CreateCell(0).SetCellValue(w.ProductCode);
            row.CreateCell(1).SetCellValue(w.ProductName);
            row.CreateCell(2).SetCellValue(w.Spec ?? "");
            row.CreateCell(3).SetCellValue(w.WarehouseName);
            row.CreateCell(4).SetCellValue(w.CurrentQty);
            row.CreateCell(5).SetCellValue(w.AvailableQty);
            row.CreateCell(6).SetCellValue(w.MinStock);
            row.CreateCell(7).SetCellValue(w.AlertLevelName);
        }

        var stream = new MemoryStream();
        workbook.Write(stream, leaveOpen: false);
        stream.Position = 0;

        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"库存预警清单_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet("InitialImport")]
    [PermissionFilter("stock:import")]
    public IActionResult InitialImport()
    {
        return View();
    }

    [HttpPost("PreviewImport")]
    [PermissionFilter("stock:import")]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "请选择文件" });

        try
        {
            var preview = await _service.PreviewAsync(file);
            return Json(new { success = true, data = preview });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"解析失败: {ex.Message}" });
        }
    }

    [HttpPost("ConfirmImport")]
    [PermissionFilter("stock:import")]
    public async Task<IActionResult> ConfirmImport([FromBody] List<InitialStockDto> items)
    {
        if (items == null || items.Count == 0)
            return Json(new { success = false, message = "无导入数据" });

        try
        {
            var result = await _service.ImportAsync(items, _user.RealName);
            return Json(new { success = result.FailCount == 0, message = result.Message, data = result });
        }
        catch (BusinessException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("DownloadTemplate")]
    [PermissionFilter("stock:import")]
    public IActionResult DownloadTemplate()
    {
        var stream = new MemoryStream();
        var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = workbook.CreateSheet("期初库存");

        // 表头
        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("设备编码");
        header.CreateCell(1).SetCellValue("仓库名称");
        header.CreateCell(2).SetCellValue("数量");

        sheet.SetColumnWidth(0, 4000);
        sheet.SetColumnWidth(1, 5000);
        sheet.SetColumnWidth(2, 3000);

        workbook.Write(stream, leaveOpen: false);
        stream.Position = 0;

        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "期初库存导入模板.xlsx");
    }
}
