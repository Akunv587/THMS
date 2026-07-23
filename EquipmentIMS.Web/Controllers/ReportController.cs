using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.BLL.Services;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.DTOs.Reports;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.Helpers;

namespace EquipmentIMS.Web.Controllers;

public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly IReportExportTaskQueue _exportQueue;
    private readonly IExportTaskStatusTracker _statusTracker;
    private readonly ICurrentUserService _user;
    private readonly IWarehouseService _warehouseService;

    public ReportController(
        IReportService reportService,
        IReportExportTaskQueue exportQueue,
        IExportTaskStatusTracker statusTracker,
        ICurrentUserService user,
        IWarehouseService warehouseService)
    {
        _reportService = reportService;
        _exportQueue = exportQueue;
        _statusTracker = statusTracker;
        _user = user;
        _warehouseService = warehouseService;
    }

    private bool IsManagerOrAdmin => _user.IsInRole("admin") || _user.IsInRole("Manager");
    private bool CanViewInventory => IsManagerOrAdmin || _user.IsInRole("Warehouse");
    private bool CanViewSale => IsManagerOrAdmin || _user.IsInRole("Sales");
    private bool CanViewRepair => IsManagerOrAdmin || _user.IsInRole("Technician");
    private bool CanViewPurchase => IsManagerOrAdmin;

    // ==================== 库存报表 ====================

    [PermissionFilter("report:view")]
    public async Task<IActionResult> Inventory(long? warehouseId, long? categoryId)
    {
        if (!CanViewInventory && !IsManagerOrAdmin) return Forbid();

        var warehouses = await _warehouseService.GetAllAsync();
        ViewData["WarehouseList"] = warehouses;
        ViewData["WarehouseId"] = warehouseId;

        var summary = await _reportService.GetInventorySummaryAsync(warehouseId, categoryId);
        var fundAnalysis = await _reportService.GetStockFundAnalysisAsync();
        var warnings = await _reportService.GetStockWarningsAsync();

        ViewData["SummaryData"] = summary;
        ViewData["FundAnalysis"] = System.Text.Json.JsonSerializer.Serialize(fundAnalysis);
        ViewData["Warnings"] = warnings;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetStockTransactions(StockTransactionQueryDto query)
    {
        if (query.Page < 1) query.Page = 1;
        if (query.PageSize < 1) query.PageSize = 20;
        var result = await _reportService.GetStockTransactionsAsync(query);
        return Json(new { rows = result.Items, total = result.TotalCount });
    }

    // ==================== 销售报表 ====================

    [PermissionFilter("report:view")]
    public async Task<IActionResult> Sale(string rankBy = "product", string groupBy = "month",
        DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!CanViewSale && !IsManagerOrAdmin) return Forbid();

        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;

        ViewData["RankBy"] = rankBy;
        ViewData["GroupBy"] = groupBy;
        ViewData["StartDate"] = start.ToString("yyyy-MM-dd");
        ViewData["EndDate"] = end.ToString("yyyy-MM-dd");

        var ranking = await _reportService.GetSaleRankingAsync(start, end, rankBy);
        var trend = await _reportService.GetSaleTrendAsync(start, end, groupBy);
        var returnStat = await _reportService.GetSaleReturnStatAsync(start, end);

        ViewData["RankingData"] = System.Text.Json.JsonSerializer.Serialize(ranking);
        ViewData["TrendData"] = System.Text.Json.JsonSerializer.Serialize(trend);
        ViewData["ReturnStat"] = System.Text.Json.JsonSerializer.Serialize(returnStat);

        return View();
    }

    // ==================== 采购报表 ====================

    [PermissionFilter("report:view")]
    public async Task<IActionResult> Purchase(string groupBy = "month",
        DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!CanViewPurchase) return Forbid();

        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;

        ViewData["GroupBy"] = groupBy;
        ViewData["StartDate"] = start.ToString("yyyy-MM-dd");
        ViewData["EndDate"] = end.ToString("yyyy-MM-dd");

        var summary = await _reportService.GetPurchaseSummaryAsync(start, end, groupBy);
        var returnStat = await _reportService.GetPurchaseReturnStatAsync(start, end);

        ViewData["SummaryData"] = System.Text.Json.JsonSerializer.Serialize(summary);
        ViewData["ReturnStat"] = System.Text.Json.JsonSerializer.Serialize(returnStat);

        return View();
    }

    // ==================== 维修报表 ====================

    [PermissionFilter("report:view")]
    public async Task<IActionResult> Repair(string statBy = "status",
        DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!CanViewRepair && !IsManagerOrAdmin) return Forbid();

        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;

        ViewData["StatBy"] = statBy;
        ViewData["StartDate"] = start.ToString("yyyy-MM-dd");
        ViewData["EndDate"] = end.ToString("yyyy-MM-dd");

        var stat = await _reportService.GetRepairStatAsync(start, end, statBy);
        var costAnalysis = await _reportService.GetRepairCostAnalysisAsync(start, end);
        var warrantyRate = await _reportService.GetWarrantyRepairRateAsync(start, end);
        var partsUsage = await _reportService.GetPartsUsageStatAsync(start, end);

        ViewData["StatData"] = System.Text.Json.JsonSerializer.Serialize(stat);
        ViewData["CostAnalysis"] = System.Text.Json.JsonSerializer.Serialize(costAnalysis);
        ViewData["WarrantyRate"] = warrantyRate;
        ViewData["PartsUsage"] = System.Text.Json.JsonSerializer.Serialize(partsUsage);

        return View();
    }

    // ==================== 同步导出 ====================

    [HttpGet]
    public async Task<IActionResult> ExportInventorySummary(long? warehouseId, long? categoryId)
    {
        var data = await _reportService.GetInventorySummaryAsync(warehouseId, categoryId);
        var bytes = ExcelExportHelper.ExportInventorySummary(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "库存汇总.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportStockWarnings()
    {
        var data = await _reportService.GetStockWarningsAsync();
        var bytes = ExcelExportHelper.ExportStockWarnings(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "库存预警.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportSaleRanking(DateTime startDate, DateTime endDate, string rankBy = "product")
    {
        var data = await _reportService.GetSaleRankingAsync(startDate, endDate, rankBy);
        var bytes = ExcelExportHelper.ExportSaleRanking(data, rankBy);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "销售排行.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportSaleTrend(DateTime startDate, DateTime endDate, string groupBy = "month")
    {
        var data = await _reportService.GetSaleTrendAsync(startDate, endDate, groupBy);
        var bytes = ExcelExportHelper.ExportSaleTrend(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "销售趋势.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportRepairStat(DateTime startDate, DateTime endDate, string statBy = "status")
    {
        var data = await _reportService.GetRepairStatAsync(startDate, endDate, statBy);
        var bytes = ExcelExportHelper.ExportRepairStat(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "维修统计.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPartsUsage(DateTime startDate, DateTime endDate)
    {
        var data = await _reportService.GetPartsUsageStatAsync(startDate, endDate);
        var bytes = ExcelExportHelper.ExportPartsUsage(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "配件使用统计.xlsx");
    }

    // ==================== 异步导出 ====================

    [HttpPost]
    public async Task<IActionResult> AsyncExport(string reportType, DateTime startDate, DateTime endDate,
        string? param1 = null, long? param2 = null)
    {
        var taskId = Guid.NewGuid();
        var task = new ReportExportTaskItem
        {
            TaskId = taskId,
            ReportType = reportType,
            ReportName = reportType switch
            {
                "InventorySummary" => "库存汇总",
                "StockFund" => "资金占用分析",
                "StockWarning" => "库存预警",
                "SaleRanking" => "销售排行",
                "SaleTrend" => "销售趋势",
                "RepairStat" => "维修统计",
                "PartsUsage" => "配件使用统计",
                _ => reportType
            },
            StartDate = startDate,
            EndDate = endDate,
            Param1 = param1,
            Param2 = param2,
            UserId = _user.UserId
        };

        _statusTracker.Create(taskId, task.ReportName);
        await _exportQueue.EnqueueAsync(task);
        return Json(new { taskId = taskId.ToString("N")[..8], message = "导出任务已提交，完成后将发送站内信通知" });
    }

    [HttpGet]
    public IActionResult GetExportStatus(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return Json(new { success = false, message = "任务ID不能为空" });

        ExportTaskDto? status = null;
        if (Guid.TryParse(taskId, out var fullGuid))
            status = _statusTracker.Get(fullGuid);

        if (status == null)
            return Json(new { success = false, message = "未找到该任务" });

        return Json(new
        {
            success = true,
            data = new
            {
                taskId = status.TaskId,
                taskName = status.TaskName,
                status = status.Status.ToString(),
                progress = status.Progress,
                fileName = status.FileName,
                downloadUrl = status.DownloadUrl,
                errorMessage = status.ErrorMessage,
                createdAt = status.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }
        });
    }
}
