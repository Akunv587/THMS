using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BackupService> _logger;

    public BackupService(AppDbContext db, IUnitOfWork uow, ILogger<BackupService> logger)
    {
        _db = db;
        _uow = uow;
        _logger = logger;
    }

    public async Task<BackupResultDto> BackupAsync(string userId, string userName)
    {
        var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        Directory.CreateDirectory(backupDir);

        var fileName = $"THMS_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var filePath = Path.Combine(backupDir, fileName);

        try
        {
            var connBuilder = new SqlConnectionStringBuilder(_db.Database.GetConnectionString()!);

            var dbName = connBuilder.InitialCatalog;
            await _db.Database.ExecuteSqlRawAsync(
                $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}' WITH FORMAT, INIT, NAME = N'THMS Full Backup'");

            var fileInfo = new FileInfo(filePath);
            var verified = await VerifyBackupFileAsync(filePath, connBuilder.InitialCatalog!);

            var history = new InvBackupHistory
            {
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                IsVerified = verified,
                CreateTime = DateTime.Now,
                CreateBy = userName
            };

            if (!verified)
                history.ErrorMessage = "备份文件验证失败";

            _db.Set<InvBackupHistory>().Add(history);
            await _db.SaveChangesAsync();

            return new BackupResultDto
            {
                Success = true,
                FileName = fileName,
                Message = "备份成功" + (verified ? "，已通过完整性验证" : "，但验证未通过"),
                Verified = verified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库备份失败");
            return new BackupResultDto
            {
                Success = false,
                Message = "备份失败: " + ex.Message
            };
        }
    }

    public async Task<List<BackupHistoryDto>> GetBackupHistoryAsync(int page, int pageSize)
    {
        return await _db.Set<InvBackupHistory>()
            .OrderByDescending(h => h.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new BackupHistoryDto
            {
                Id = h.Id,
                FileName = h.FileName,
                FileSize = h.FileSize,
                FileSizeStr = FormatFileSize(h.FileSize),
                IsVerified = h.IsVerified,
                ErrorMessage = h.ErrorMessage,
                CreateTime = h.CreateTime,
                CreateBy = h.CreateBy
            })
            .ToListAsync();
    }

    public async Task<bool> VerifyBackupAsync(long historyId)
    {
        var history = await _db.Set<InvBackupHistory>().FirstOrDefaultAsync(h => h.Id == historyId);
        if (history == null) return false;

        var connBuilder = new SqlConnectionStringBuilder(_db.Database.GetConnectionString()!);
        var verified = await VerifyBackupFileAsync(history.FilePath, connBuilder.InitialCatalog!);

        history.IsVerified = verified;
        if (!verified) history.ErrorMessage = "验证失败";
        await _db.SaveChangesAsync();

        return verified;
    }

    public async Task<string?> GetBackupFilePathAsync(long id)
    {
        var history = await _db.Set<InvBackupHistory>().FirstOrDefaultAsync(h => h.Id == id);
        return history?.FilePath;
    }

    private async Task<bool> VerifyBackupFileAsync(string filePath, string dbName)
    {
        try
        {
            var connBuilder = new SqlConnectionStringBuilder(_db.Database.GetConnectionString()!)
            {
                InitialCatalog = "master"
            };
            using var conn = new SqlConnection(connBuilder.ConnectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"RESTORE VERIFYONLY FROM DISK = @filePath";
            cmd.Parameters.AddWithValue("@filePath", filePath);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int unitIndex = 0;
        double size = bytes;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:F2} {units[unitIndex]}";
    }
}
