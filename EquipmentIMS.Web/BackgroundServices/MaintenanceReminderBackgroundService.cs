using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Web.BackgroundServices;

public class MaintenanceReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MaintenanceReminderBackgroundService> _logger;

    public MaintenanceReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MaintenanceReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayToNextRun();
            _logger.LogInformation("下次保养提醒检查时间: {Time}", DateTime.Now.Add(delay));
            await Task.Delay(delay, stoppingToken);

            try
            {
                await CheckAndNotifyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保养提醒检查失败");
            }
        }
    }

    private static TimeSpan CalculateDelayToNextRun()
    {
        var now = DateTime.Now;
        var target = now.Date.AddHours(8);
        if (now >= target)
            target = target.AddDays(1);
        return target - now;
    }

    private async Task CheckAndNotifyAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateTime.Today;
        var plans = await uow.GetRepository<MaintMaintenancePlan>().Query()
            .Include(e => e.DeviceArchive).ThenInclude(e => e.Product)
            .Where(e => !e.IsDeleted && e.IsActive && e.NextDate <= today)
            .ToListAsync(ct);

        if (plans.Count == 0)
        {
            _logger.LogInformation("无到期保养计划");
            return;
        }

        // 查询具有 admin 或 manager 角色的活跃用户
        var targetRoleIds = await db.SysRoles
            .Where(r => (r.RoleCode == "admin" || r.RoleCode == "manager") && !r.IsDeleted)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var targetUserIds = targetRoleIds.Count > 0
            ? await db.SysUserRoles
                .Where(ur => targetRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(ct)
            : new List<long>();

        var techs = targetUserIds.Count > 0
            ? await uow.GetRepository<SysUser>().Query()
                .Where(u => targetUserIds.Contains(u.Id) && u.IsActive && !u.IsDeleted)
                .ToListAsync(ct)
            : await uow.GetRepository<SysUser>().Query()
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync(ct);

        foreach (var plan in plans)
        {
            foreach (var tech in techs)
            {
                try
                {
                    var deviceName = plan.DeviceArchive?.SerialNo ?? plan.DeviceArchiveId.ToString();
                    var productName = plan.DeviceArchive?.Product?.Name ?? "";
                    var title = $"【保养提醒】{plan.PlanName} - {deviceName}";
                    var content = $"设备 {deviceName}（{productName}）的保养计划「{plan.PlanName}」已到期，请及时处理。\n下次保养日期：{plan.NextDate:yyyy-MM-dd}";

                    await notificationService.SendAsync(tech.Id, title, content,
                        NotificationType.MaintenanceReminder, "MaintenancePlan", plan.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送保养提醒失败 PlanId={PlanId} UserId={UserId}", plan.Id, tech.Id);
                }
            }

            plan.NextDate = plan.NextDate.AddDays(plan.CycleDays);
            plan.UpdateTime = DateTime.Now;
            uow.GetRepository<MaintMaintenancePlan>().Update(plan);
        }

        await uow.SaveChangesAsync();
        _logger.LogInformation("保养提醒已完成，处理 {Count} 个到期计划", plans.Count);
    }
}
