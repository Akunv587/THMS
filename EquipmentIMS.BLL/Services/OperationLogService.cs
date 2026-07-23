using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class OperationLogService : IOperationLogService
{
    private readonly AppDbContext _db;
    private readonly IOperationLogRepository _logRepo;

    public OperationLogService(AppDbContext db, IOperationLogRepository logRepo)
    {
        _db = db;
        _logRepo = logRepo;
    }

    public async Task<PagedResult<OperationLogDto>> GetListAsync(OperationLogQueryDto query)
    {
        var q = _db.SysOperationLogs.AsQueryable();

        if (query.StartTime.HasValue)
            q = q.Where(l => l.CreateTime >= query.StartTime.Value);
        if (query.EndTime.HasValue)
            q = q.Where(l => l.CreateTime <= query.EndTime.Value);
        if (!string.IsNullOrEmpty(query.Module))
            q = q.Where(l => l.Module == query.Module);
        if (query.UserId.HasValue)
            q = q.Where(l => l.UserId == query.UserId.Value);
        if (!string.IsNullOrEmpty(query.Keyword))
            q = q.Where(l => l.UserName.Contains(query.Keyword) || l.Description!.Contains(query.Keyword));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(l => l.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new OperationLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.UserName,
                Module = l.Module,
                Action = l.Action,
                Description = l.Description,
                IP = l.IP,
                CreateTime = l.CreateTime
            })
            .ToListAsync();

        return PagedResult<OperationLogDto>.Create(items, total, query.Page, query.PageSize);
    }

    public async Task<byte[]> ExportAsync(OperationLogQueryDto query)
    {
        query.Page = 1;
        query.PageSize = 100000;
        var result = await GetListAsync(query);

        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("操作日志");
        var header = sheet.CreateRow(0);
        var headers = new[] { "序号", "用户", "模块", "操作", "描述", "IP地址", "操作时间" };
        for (int i = 0; i < headers.Length; i++)
            header.CreateCell(i).SetCellValue(headers[i]);

        int rowIndex = 1;
        foreach (var item in result.Items)
        {
            var row = sheet.CreateRow(rowIndex++);
            row.CreateCell(0).SetCellValue(rowIndex - 1);
            row.CreateCell(1).SetCellValue(item.UserName);
            row.CreateCell(2).SetCellValue(item.Module);
            row.CreateCell(3).SetCellValue(item.Action);
            row.CreateCell(4).SetCellValue(item.Description ?? "");
            row.CreateCell(5).SetCellValue(item.IP ?? "");
            row.CreateCell(6).SetCellValue(item.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    public async Task<List<OperationLogDto>> GetArchiveListAsync(OperationLogQueryDto query)
    {
        // 归档日志仅返回超过6个月的数据
        var archiveDate = DateTime.Now.AddMonths(-6);
        query.StartTime = null;
        query.EndTime = archiveDate;
        query.Page = 1;
        query.PageSize = int.MaxValue;
        var result = await GetListAsync(query);
        return result.Items;
    }
}
