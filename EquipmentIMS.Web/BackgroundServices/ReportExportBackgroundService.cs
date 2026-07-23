using NPOI.XSSF.UserModel;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.BLL.Services;
using EquipmentIMS.Models.DTOs.Reports;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Web.Helpers;

namespace EquipmentIMS.Web.BackgroundServices;

public class ReportExportBackgroundService : BackgroundService
{
    private readonly IReportExportTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportExportBackgroundService> _logger;
    private readonly IExportTaskStatusTracker _statusTracker;

    public ReportExportBackgroundService(
        IReportExportTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ReportExportBackgroundService> logger,
        IExportTaskStatusTracker statusTracker)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _statusTracker = statusTracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                await ProcessTaskAsync(task, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报表导出任务 {TaskId} 处理失败", task.TaskId);
                await NotifyAsync(task.UserId, task.TaskId, $"报表导出失败：{ex.Message}", success: false);
            }
        }
    }

    private async Task ProcessTaskAsync(ReportExportTaskItem task, CancellationToken ct)
    {
        _logger.LogInformation("开始处理报表导出 {TaskId}, 类型={ReportType}", task.TaskId, task.ReportType);
        _statusTracker.SetProcessing(task.TaskId);

        using var scope = _scopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

        byte[] fileBytes;
        string fileNameSuffix;

        switch (task.ReportType)
        {
            case "InventorySummary":
                var invData = await reportService.GetInventorySummaryAsync(task.Param2, null);
                fileBytes = ExcelExportHelper.ExportInventorySummary(invData);
                fileNameSuffix = "库存汇总";
                break;
            case "StockFund":
                var fundData = await reportService.GetStockFundAnalysisAsync();
                fileBytes = ExcelExportHelper.ExportStockFund(fundData);
                fileNameSuffix = "资金占用分析";
                break;
            case "StockWarning":
                var warnData = await reportService.GetStockWarningsAsync();
                fileBytes = ExcelExportHelper.ExportStockWarnings(warnData);
                fileNameSuffix = "库存预警";
                break;
            case "SaleRanking":
                var rankData = await reportService.GetSaleRankingAsync(task.StartDate, task.EndDate, task.Param1 ?? "product");
                fileBytes = ExcelExportHelper.ExportSaleRanking(rankData, task.Param1 ?? "product");
                fileNameSuffix = "销售排行";
                break;
            case "SaleTrend":
                var trendData = await reportService.GetSaleTrendAsync(task.StartDate, task.EndDate, task.Param1 ?? "month");
                fileBytes = ExcelExportHelper.ExportSaleTrend(trendData);
                fileNameSuffix = "销售趋势";
                break;
            case "RepairStat":
                var statData = await reportService.GetRepairStatAsync(task.StartDate, task.EndDate, task.Param1 ?? "status");
                fileBytes = ExcelExportHelper.ExportRepairStat(statData);
                fileNameSuffix = "维修统计";
                break;
            case "PartsUsage":
                var partsData = await reportService.GetPartsUsageStatAsync(task.StartDate, task.EndDate);
                fileBytes = ExcelExportHelper.ExportPartsUsage(partsData);
                fileNameSuffix = "配件使用统计";
                break;
            case "PurchaseSummary":
                var purData = await reportService.GetPurchaseSummaryAsync(task.StartDate, task.EndDate, task.Param1 ?? "month");
                fileBytes = ExcelExportHelper.ExportPurchaseSummary(purData);
                fileNameSuffix = "采购汇总";
                break;
            default:
                await NotifyAsync(task.UserId, task.TaskId, $"未知报表类型: {task.ReportType}", success: false);
                return;
        }

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports");
        Directory.CreateDirectory(dir);
        var fileName = $"{task.TaskId}_{fileNameSuffix}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        var filePath = Path.Combine(dir, fileName);
        await File.WriteAllBytesAsync(filePath, fileBytes, ct);

        var downloadUrl = $"/exports/{fileName}";
        await NotifyAsync(task.UserId, task.TaskId,
            $"报表「{fileNameSuffix}」导出完成。<a href=\"{downloadUrl}\" target=\"_blank\">点击下载</a>",
            success: true);

        _logger.LogInformation("报表导出任务 {TaskId} 完成", task.TaskId);
    }

    private async Task NotifyAsync(long userId, Guid taskId, string content, bool success, string? fileName = null, string? downloadUrl = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notification.SendAsync(userId,
                success ? "导出完成" : "导出失败",
                content, NotificationType.SystemNotification);

            if (success && fileName != null && downloadUrl != null)
                _statusTracker.SetCompleted(taskId, fileName, downloadUrl);
            else if (!success)
                _statusTracker.SetFailed(taskId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送导出通知失败");
            _statusTracker.SetFailed(taskId, ex.Message);
        }
    }
}
