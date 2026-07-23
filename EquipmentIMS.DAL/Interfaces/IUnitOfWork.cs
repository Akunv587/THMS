using Microsoft.EntityFrameworkCore.Storage;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 工作单元接口，统一管理事务和仓储
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 获取泛型仓储（仅限继承 BaseEntity 的实体）
    /// </summary>
    IRepository<T> GetRepository<T>() where T : BaseEntity;

    IStockRepository Stocks { get; }
    IStockTransactionRepository StockTransactions { get; }
    ISaleOrderRepository SaleOrders { get; }
    IPurchaseOrderRepository PurchaseOrders { get; }
    IOperationLogRepository OperationLogs { get; }

    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();

    /// <summary>
    /// 在重试执行策略中执行事务操作（解决 EnableRetryOnFailure 不支持用户事务的问题）
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action);
}
