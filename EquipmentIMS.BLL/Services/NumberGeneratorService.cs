using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;

namespace EquipmentIMS.BLL.Services;

/// <summary>
/// 编号生成服务，通过 sp_getapplock 保证并发安全
/// </summary>
public class NumberGeneratorService : INumberGeneratorService
{
    private readonly AppDbContext _context;
    private const int MaxRetry = 3;

    public NumberGeneratorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(string prefix, string tableName, string columnName)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var resourceName = $"NR_{prefix}";

        for (var retry = 0; retry < MaxRetry; retry++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_getapplock @Resource={0}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000",
                    resourceName);

                var likePattern = $"{prefix}-{date}-%";
                var sql = string.Format(
                    "SELECT MAX(CAST(SUBSTRING([{0}], LEN('{1}-{2}-') + 1, 4) AS INT)) FROM [{3}] WITH(NOLOCK) WHERE [{0}] LIKE @p0",
                    columnName, prefix, date, tableName);
                var result = await _context.Database
                    .SqlQueryRaw<int?>(sql, likePattern)
                    .FirstOrDefaultAsync();

                var maxNo = (result ?? 0) + 1;
                var number = $"{prefix}-{date}-{maxNo:D4}";
                await transaction.CommitAsync();
                return number;
            }
            catch (Exception) when (retry < MaxRetry - 1)
            {
                await transaction.RollbackAsync();
                await Task.Delay(Random.Shared.Next(50, 200));
            }
        }

        throw new InvalidOperationException($"生成编号失败，已重试{MaxRetry}次");
    }

    public async Task<string> GenerateWithTimeAsync(string prefix, string tableName, string columnName)
    {
        var dateTimePart = DateTime.Now.ToString("yyyyMMddHHmmss");
        var resourceName = $"NR_{prefix}";

        for (var retry = 0; retry < MaxRetry; retry++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_getapplock @Resource={0}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000",
                    resourceName);

                var likePattern = $"{prefix}-{dateTimePart}-%";
                var sql = string.Format(
                    "SELECT MAX(CAST(SUBSTRING([{0}], LEN('{1}-{2}-') + 1, 3) AS INT)) FROM [{3}] WHERE [{0}] LIKE @p0",
                    columnName, prefix, dateTimePart, tableName);
                var result = await _context.Database
                    .SqlQueryRaw<int?>(sql, likePattern)
                    .FirstOrDefaultAsync();

                var maxNo = (result ?? 0) + 1;
                var number = $"{prefix}-{dateTimePart}-{maxNo:D3}";
                await transaction.CommitAsync();
                return number;
            }
            catch (Exception) when (retry < MaxRetry - 1)
            {
                await transaction.RollbackAsync();
                await Task.Delay(Random.Shared.Next(50, 200));
            }
        }

        throw new InvalidOperationException($"生成编号失败，已重试{MaxRetry}次");
    }
}
