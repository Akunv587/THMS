using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[Route("[controller]")]
public class LogController : Controller
{
    private readonly IOperationLogService _service;

    public LogController(IOperationLogService service) => _service = service;

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(OperationLogQueryDto query)
    {
        var result = await _service.GetListAsync(query);
        ViewData["Keyword"] = query.Keyword;
        ViewData["Module"] = query.Module;
        ViewData["StartTime"] = query.StartTime?.ToString("yyyy-MM-dd");
        ViewData["EndTime"] = query.EndTime?.ToString("yyyy-MM-dd");
        ViewData["Page"] = result.Page;
        ViewData["PageSize"] = result.PageSize;
        ViewData["TotalCount"] = result.TotalCount;
        return View(result);
    }

    [HttpGet("Export")]
    public async Task<IActionResult> Export(OperationLogQueryDto query)
    {
        var bytes = await _service.ExportAsync(query);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"操作日志_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
