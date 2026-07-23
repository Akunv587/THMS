using EquipmentIMS.BLL.Interfaces;

namespace EquipmentIMS.Web.BackgroundServices;

public class StockAlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockAlertBackgroundService> _logger;

    public StockAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<StockAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await CalculateDelayToNextRunAsync(stoppingToken);
            _logger.LogInformation("下次库存预警检查时间: {Time}", DateTime.Now.Add(delay));
            await Task.Delay(delay, stoppingToken);

            try
            {
                await CheckAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "库存预警检查执行失败");
            }
        }
    }

    private async Task<TimeSpan> CalculateDelayToNextRunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();
            var cronConfig = await configService.GetAsync("StockAlertCheckCron") ?? "08:00";

            var hour = 8;
            var minute = 0;
            var parts = cronConfig.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                hour = Math.Clamp(h, 0, 23);
                minute = Math.Clamp(m, 0, 59);
            }

            var now = DateTime.Now;
            var target = now.Date.AddHours(hour).AddMinutes(minute);
            if (now >= target)
                target = target.AddDays(1);
            return target - now;
        }
        catch
        {
            var now = DateTime.Now;
            var target = now.Date.AddHours(8);
            if (now >= target)
                target = target.AddDays(1);
            return target - now;
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
        await stockService.CheckAndCreateAlertsAsync();
        _logger.LogInformation("库存预警检查完成");
    }
}
