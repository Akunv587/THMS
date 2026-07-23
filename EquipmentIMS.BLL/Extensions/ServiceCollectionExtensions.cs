using Microsoft.Extensions.DependencyInjection;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.BLL.Services;

namespace EquipmentIMS.BLL.Extensions;

/// <summary>
/// BLL 层依赖注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册所有 BLL 层服务
    /// </summary>
    public static IServiceCollection AddBLLServices(this IServiceCollection services)
    {
        // 用户上下文
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 编号生成
        services.AddScoped<INumberGeneratorService, NumberGeneratorService>();

        // 缓存
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // 通知
        services.AddScoped<INotificationService, NotificationService>();

        // 系统配置
        services.AddScoped<ISystemConfigService, SystemConfigService>();

        // 菜单
        services.AddScoped<IMenuService, MenuService>();

        // 仪表盘
        services.AddScoped<IDashboardService, DashboardService>();

        // 基础数据
        services.AddSingleton<IAesEncryptionService, AesEncryptionService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IProductService, ProductService>();

        // 采购
        services.AddScoped<IPurchaseService, PurchaseService>();

        // 销售
        services.AddScoped<ISaleService, SaleService>();

        // 库存
        services.AddScoped<IStockService, StockService>();

        // 维修
        services.AddScoped<IDeviceArchiveService, DeviceArchiveService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();

        // 导出任务队列（单例，跨 StockService 和 BackgroundService 共享）
        services.AddSingleton<IExportTaskQueue, ExportTaskQueue>();
        services.AddSingleton<IReportExportTaskQueue, ReportExportTaskQueue>();

        // 导出任务状态跟踪器（单例）
        services.AddSingleton<IExportTaskStatusTracker, ExportTaskStatusTracker>();

        // 报表
        services.AddScoped<IReportService, ReportService>();

        // 系统管理
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IOperationLogService, OperationLogService>();
        services.AddScoped<IBackupService, BackupService>();

        return services;
    }
}
