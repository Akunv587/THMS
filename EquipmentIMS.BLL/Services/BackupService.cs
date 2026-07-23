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
        var fileName = $"THMS_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

        try
        {
            var connBuilder = new SqlConnectionStringBuilder(_db.Database.GetConnectionString()!);
            var dbName = connBuilder.InitialCatalog;

            // 确定备份目录：优先使用 ProgramData 共享目录，失败回退到 SQL Server 默认备份路径
            var backupDir = await ResolveBackupDirectoryAsync();
            Directory.CreateDirectory(backupDir);

            var filePath = Path.Combine(backupDir, fileName);

            await _db.Database.ExecuteSqlRawAsync(
                $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}' WITH FORMAT, INIT, NAME = N'THMS Full Backup'");

            // 验证备份文件
            var fileInfo = new FileInfo(filePath);
            var verified = await VerifyBackupFileAsync(filePath, dbName);

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

            // 主路径失败时，尝试使用 SQL Server 默认备份路径
            try
            {
                var sqlBackupDir = await GetSqlDefaultBackupPathAsync();
                var sqlFilePath = Path.Combine(sqlBackupDir, fileName);

                var connBuilder = new SqlConnectionStringBuilder(_db.Database.GetConnectionString()!);
                var dbName = connBuilder.InitialCatalog;

                await _db.Database.ExecuteSqlRawAsync(
                    $"BACKUP DATABASE [{dbName}] TO DISK = N'{sqlFilePath}' WITH FORMAT, INIT, NAME = N'THMS Full Backup'");

                var fileInfo = new FileInfo(sqlFilePath);
                var verified = await VerifyBackupFileAsync(sqlFilePath, dbName);

                var history = new InvBackupHistory
                {
                    FileName = fileName,
                    FilePath = sqlFilePath,
                    FileSize = fileInfo.Length,
                    IsVerified = verified,
                    CreateTime = DateTime.Now,
                    CreateBy = userName
                };

                if (!verified)
                    history.ErrorMessage = "备份文件验证失败";

                _db.Set<InvBackupHistory>().Add(history);
                await _db.SaveChangesAsync();

                _logger.LogInformation("已回退到 SQL Server 默认备份路径完成备份: {Path}", sqlFilePath);
                return new BackupResultDto
                {
                    Success = true,
                    FileName = fileName,
                    Message = "备份成功" + (verified ? "，已通过完整性验证" : "，但验证未通过") + "（使用SQL Server默认路径）",
                    Verified = verified
                };
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "回退备份路径也失败");
                return new BackupResultDto
                {
                    Success = false,
                    Message = "备份失败: " + ex.Message
                };
            }
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
            cmd.CommandText = "RESTORE VERIFYONLY FROM DISK = @filePath";
            cmd.Parameters.AddWithValue("@filePath", filePath);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析备份目标目录：优先使用 ProgramData 共享目录（已赋权 SQL Server 服务账户）
    /// </summary>
    private async Task<string> ResolveBackupDirectoryAsync()
    {
        // 首选：ProgramData 共享目录（已通过 icacls 赋权 NT Service\MSSQLSERVER）
        var programDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "THMS", "Backups");

        // 备选：当前工作目录下的 Backups（用于本地开发/回退场景）
        var localDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

        // 确保首选目录存在（已提前创建并赋权）
        if (!Directory.Exists(programDataDir))
        {
            try
            {
                Directory.CreateDirectory(programDataDir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法创建 ProgramData 备份目录，回退到本地目录");
                Directory.CreateDirectory(localDir);
                return localDir;
            }
        }

        return programDataDir;
    }

    /// <summary>
    /// 获取 SQL Server 实例的默认备份路径
    /// </summary>
    private async Task<string> GetSqlDefaultBackupPathAsync()
    {
        try
        {
            var connStr = _db.Database.GetConnectionString();
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // SQL Server 2019+ 支持 InstanceDefaultBackupPath
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ISNULL(CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260)), N'')";
            var result = (await cmd.ExecuteScalarAsync()) as string;
            if (!string.IsNullOrEmpty(result) && Directory.Exists(result))
                return result;

            // 回退：使用 InstanceDefaultDataPath 的上级目录 + Backup
            cmd.CommandText = "SELECT ISNULL(CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(260)), N'')";
            result = (await cmd.ExecuteScalarAsync()) as string;
            if (!string.IsNullOrEmpty(result))
            {
                var parent = Directory.GetParent(result)?.FullName;
                if (parent != null)
                {
                    var backupDir = Path.Combine(parent, "Backup");
                    if (Directory.Exists(backupDir))
                        return backupDir;
                }
            }

            // 最终回退：SQL Server 安装路径下的 Backup 目录
            var sqlRoot = @"E:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup";
            if (Directory.Exists(sqlRoot))
                return sqlRoot;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询 SQL Server 默认备份路径失败");
        }

        // 兜底：临时目录
        return Path.Combine(Path.GetTempPath(), "THMS_Backups");
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
