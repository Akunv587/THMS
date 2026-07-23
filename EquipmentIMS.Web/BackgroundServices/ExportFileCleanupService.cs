namespace EquipmentIMS.Web.BackgroundServices;

/// <summary>
/// 导出文件清理后台服务：每天凌晨清理 wwwroot/exports/ 目录下超过 7 天的文件
/// </summary>
public class ExportFileCleanupService : BackgroundService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExportFileCleanupService> _logger;

    public ExportFileCleanupService(IWebHostEnvironment env, ILogger<ExportFileCleanupService> logger)
    {
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("导出文件清理服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var exportsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "exports");
                if (Directory.Exists(exportsDir))
                {
                    var cutoff = DateTime.Now.AddDays(-7);
                    var deletedCount = 0;

                    foreach (var file in Directory.GetFiles(exportsDir))
                    {
                        if (File.GetCreationTime(file) < cutoff)
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                    }

                    if (deletedCount > 0)
                        _logger.LogInformation("已清理 {Count} 个过期导出文件（超过 7 天）", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出文件清理失败");
            }

            // 每 24 小时执行一次
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        _logger.LogInformation("导出文件清理服务已停止");
    }
}
