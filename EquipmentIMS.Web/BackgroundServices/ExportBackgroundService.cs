using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using EquipmentIMS.BLL.Services;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.BLL.Interfaces;

namespace EquipmentIMS.Web.BackgroundServices;

public class ExportBackgroundService : BackgroundService
{
    private readonly IExportTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportBackgroundService> _logger;

    public ExportBackgroundService(IExportTaskQueue queue, IServiceScopeFactory scopeFactory, ILogger<ExportBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                _logger.LogError(ex, "导出任务 {TaskId} 处理失败", task.TaskId);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notification.SendAsync(task.UserId, "导出失败",
                        $"库存流水导出任务 {task.TaskId} 处理失败，请联系管理员。", NotificationType.SystemNotification);
                }
                catch { }
            }
        }
    }

    private async Task ProcessTaskAsync(ExportTaskItem task, CancellationToken ct)
    {
        _logger.LogInformation("开始处理导出任务 {TaskId}", task.TaskId);

        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var repo = uow.StockTransactions;
        var q = task.Query;

        // 关键字搜索：解析为产品ID列表
        List<long>? keywordProductIds = null;
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            keywordProductIds = await uow.GetRepository<BaseProduct>().Query()
                .Where(p => p.Code.Contains(q.Keyword) || p.Name.Contains(q.Keyword))
                .Select(p => p.Id).ToListAsync(ct);
            if (keywordProductIds.Count == 0)
            {
                await notification.SendAsync(task.UserId, "导出完成",
                    $"库存流水导出任务 {task.TaskId} 完成，查询结果为空。", NotificationType.SystemNotification);
                return;
            }
        }

        long? effectiveProductId = q.ProductId;
        if (keywordProductIds is { Count: 1 } && !q.ProductId.HasValue)
            effectiveProductId = keywordProductIds[0];

        var total = await repo.CountAsync(q.StartTime, q.EndTime, effectiveProductId, q.WarehouseId,
            q.TransactionType.HasValue ? (TransactionType?)q.TransactionType.Value : null);

        if (total == 0)
        {
            await notification.SendAsync(task.UserId, "导出完成",
                $"库存流水导出任务 {task.TaskId} 完成，查询结果为空。", NotificationType.SystemNotification);
            return;
        }

        var data = (await repo.QueryAsync(q.StartTime, q.EndTime, effectiveProductId, q.WarehouseId,
            q.TransactionType.HasValue ? (TransactionType?)q.TransactionType.Value : null,
            1, total + 1)).ToList();

        // 如果 keyword 匹配多个产品，在内存中过滤
        if (keywordProductIds is { Count: > 1 } || (keywordProductIds is { Count: 1 } && q.ProductId.HasValue))
        {
            var ids = keywordProductIds!.ToHashSet();
            data = data.Where(t => ids.Contains(t.ProductId)).ToList();
        }

        var productIds = data.Select(t => t.ProductId).Distinct().ToList();
        var warehouseIds = data.Select(t => t.WarehouseId).Distinct().ToList();

        var products = await uow.GetRepository<BaseProduct>().Query()
            .Where(p => productIds.Contains(p.Id)).ToListAsync(ct);
        var warehouses = await uow.GetRepository<BaseWarehouse>().Query()
            .Where(w => warehouseIds.Contains(w.Id)).ToListAsync(ct);

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{task.TaskId}_库存流水_{DateTime.Now:yyyyMMddHHmmss}.xlsx");

        using var stream = new MemoryStream();
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("库存流水");

        var styleHeader = workbook.CreateCellStyle();
        var fontHeader = workbook.CreateFont();
        fontHeader.IsBold = true;
        styleHeader.SetFont(fontHeader);

        var headers = new[] { "流水号", "时间", "类型", "设备编码", "设备名称", "仓库", "数量", "变动后库存", "关联单据", "操作人", "备注" };
        var headerRow = sheet.CreateRow(0);
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = styleHeader;
        }

        for (int i = 0; i < data.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var t = data[i];
            var row = sheet.CreateRow(i + 1);
            row.CreateCell(0).SetCellValue(t.TransactionNo);
            row.CreateCell(1).SetCellValue(t.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            row.CreateCell(2).SetCellValue(GetTypeName(t.TransactionType));
            row.CreateCell(3).SetCellValue(products.FirstOrDefault(p => p.Id == t.ProductId)?.Code ?? "");
            row.CreateCell(4).SetCellValue(products.FirstOrDefault(p => p.Id == t.ProductId)?.Name ?? "");
            row.CreateCell(5).SetCellValue(warehouses.FirstOrDefault(w => w.Id == t.WarehouseId)?.Name ?? "");
            row.CreateCell(6).SetCellValue(t.Qty);
            row.CreateCell(7).SetCellValue(t.BalanceQty);
            row.CreateCell(8).SetCellValue(t.RefNo ?? "");
            row.CreateCell(9).SetCellValue(t.Operator);
            row.CreateCell(10).SetCellValue(t.Remark ?? "");
        }

        for (int i = 0; i < headers.Length; i++)
            sheet.SetColumnWidth(i, 5000);

        workbook.Write(stream, leaveOpen: false);

        await File.WriteAllBytesAsync(filePath, stream.ToArray(), ct);

        var downloadUrl = $"/exports/{Path.GetFileName(filePath)}";
        await notification.SendAsync(task.UserId, "导出完成",
            $"库存流水导出任务已完成，共 {total} 条记录。<a href=\"{downloadUrl}\" target=\"_blank\">点击下载</a>",
            NotificationType.SystemNotification);

        _logger.LogInformation("导出任务 {TaskId} 完成，文件: {Path}", task.TaskId, filePath);
    }

    private static string GetTypeName(TransactionType type) => type switch
    {
        TransactionType.PurchaseIn => "采购入库",
        TransactionType.PurchaseReturnOut => "采购退货出库",
        TransactionType.SaleOut => "销售出库",
        TransactionType.SaleReturnIn => "销售退货入库",
        TransactionType.TransferIn => "调拨入库",
        TransactionType.TransferOut => "调拨出库",
        TransactionType.StockGain => "盘盈入库",
        TransactionType.StockLoss => "盘亏出库",
        TransactionType.InitialImport => "初始导入",
        TransactionType.RepairUse => "维修领用",
        _ => type.ToString()
    };
}
