using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Repositories;

/// <summary>
/// 工作单元实现，管理所有仓储和事务
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    private IStockRepository? _stocks;
    private IStockTransactionRepository? _stockTransactions;
    private ISaleOrderRepository? _saleOrders;
    private IPurchaseOrderRepository? _purchaseOrders;
    private IOperationLogRepository? _operationLogs;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<T> GetRepository<T>() where T : BaseEntity
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new GenericRepository<T>(_context));
    }

    public IStockRepository Stocks => _stocks ??= new StockRepository(_context);
    public IStockTransactionRepository StockTransactions => _stockTransactions ??= new StockTransactionRepository(_context);
    public ISaleOrderRepository SaleOrders => _saleOrders ??= new SaleOrderRepository(_context);
    public IPurchaseOrderRepository PurchaseOrders => _purchaseOrders ??= new PurchaseOrderRepository(_context);
    public IOperationLogRepository OperationLogs => _operationLogs ??= new OperationLogRepository(_context);

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await action();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
