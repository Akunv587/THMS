using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[Route("[controller]")]
[PermissionFilter("stock:view")]
public class StockTransactionController : Controller
{
    private readonly IStockService _service;
    private readonly ICurrentUserService _user;

    public StockTransactionController(IStockService service, ICurrentUserService user)
    {
        _service = service;
        _user = user;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20,
        DateTime? startTime = null, DateTime? endTime = null,
        long? productId = null, long? warehouseId = null, int? transactionType = null, string? keyword = null)
    {
        var query = new StockTransactionQueryDto
        {
            Page = page,
            PageSize = pageSize,
            StartTime = startTime,
            EndTime = endTime,
            ProductId = productId,
            WarehouseId = warehouseId,
            TransactionType = transactionType,
            Keyword = keyword
        };

        var result = await _service.GetTransactionsAsync(query);

        ViewData["Warehouses"] = await _service.GetWarehousesAsync();
        ViewData["Page"] = result.Page;
        ViewData["PageSize"] = result.PageSize;
        ViewData["TotalCount"] = result.TotalCount;
        ViewData["ActionName"] = "Index";
        ViewData["ControllerName"] = "StockTransaction";
        ViewData["RouteValues"] = new Dictionary<string, object?>
        {
            ["startTime"] = startTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            ["endTime"] = endTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            ["productId"] = productId,
            ["warehouseId"] = warehouseId,
            ["transactionType"] = transactionType,
            ["keyword"] = keyword
        };

        return View(result.Items);
    }

    [HttpGet("Export")]
    public async Task<IActionResult> Export(DateTime? startTime = null, DateTime? endTime = null,
        long? productId = null, long? warehouseId = null, int? transactionType = null, string? keyword = null)
    {
        try
        {
            var query = new StockTransactionQueryDto
            {
                Page = 1,
                PageSize = int.MaxValue,
                StartTime = startTime,
                EndTime = endTime,
                ProductId = productId,
                WarehouseId = warehouseId,
                TransactionType = transactionType,
                Keyword = keyword
            };

            var excel = await _service.ExportTransactionsAsync(query);
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"库存流水_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("AsyncExport")]
    public async Task<IActionResult> AsyncExport(DateTime? startTime = null, DateTime? endTime = null,
        long? productId = null, long? warehouseId = null, int? transactionType = null, string? keyword = null)
    {
        var query = new StockTransactionQueryDto
        {
            Page = 1,
            PageSize = int.MaxValue,
            StartTime = startTime,
            EndTime = endTime,
            ProductId = productId,
            WarehouseId = warehouseId,
            TransactionType = transactionType,
            Keyword = keyword
        };

        var taskId = await _service.StartExportTaskAsync(query, _user.UserId);
        return Json(new { success = true, taskId = taskId, message = $"导出任务已提交，任务ID: {taskId}" });
    }

    [HttpGet("CheckExport/{taskId}")]
    public async Task<IActionResult> CheckExport(Guid taskId)
    {
        var path = await _service.GetExportFilePathAsync(taskId);
        if (path == null)
            return Json(new { success = false, message = "导出任务处理中..." });

        var fileName = Path.GetFileName(path);
        var downloadUrl = Url.Content($"~/exports/{fileName}");
        return Json(new { success = true, downloadUrl });
    }
}
