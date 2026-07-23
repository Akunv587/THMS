using Microsoft.Extensions.DependencyInjection;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.DAL.Repositories;

namespace EquipmentIMS.DAL.Extensions;

/// <summary>
/// DAL 层依赖注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册所有 DAL 层服务
    /// </summary>
    public static IServiceCollection AddDALServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
        services.AddScoped<ISaleOrderRepository, SaleOrderRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IOperationLogRepository, OperationLogRepository>();

        return services;
    }
}
