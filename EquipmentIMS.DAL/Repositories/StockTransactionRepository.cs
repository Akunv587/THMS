using Microsoft.EntityFrameworkCore;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.DAL.Repositories;

/// <summary>
/// 库存流水仓储实现
/// </summary>
public class StockTransactionRepository : IStockTransactionRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<InvStockTransaction> _dbSet;

    public StockTransactionRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<InvStockTransaction>();
    }

    public async Task AddAsync(InvStockTransaction transaction)
    {
        await _dbSet.AddAsync(transaction);
    }

    public async Task<IEnumerable<InvStockTransaction>> QueryAsync(DateTime? startTime, DateTime? endTime,
        long? productId, long? warehouseId, TransactionType? type, int page, int pageSize)
    {
        var query = BuildQuery(startTime, endTime, productId, warehouseId, type);
        return await query
            .OrderByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountAsync(DateTime? startTime, DateTime? endTime,
        long? productId, long? warehouseId, TransactionType? type)
    {
        var query = BuildQuery(startTime, endTime, productId, warehouseId, type);
        return await query.AsNoTracking().CountAsync();
    }

    private IQueryable<InvStockTransaction> BuildQuery(DateTime? startTime, DateTime? endTime,
        long? productId, long? warehouseId, TransactionType? type)
    {
        var query = _dbSet.AsQueryable();
        if (startTime.HasValue) query = query.Where(t => t.CreateTime >= startTime.Value);
        if (endTime.HasValue) query = query.Where(t => t.CreateTime <= endTime.Value);
        if (productId.HasValue) query = query.Where(t => t.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(t => t.WarehouseId == warehouseId.Value);
        if (type.HasValue) query = query.Where(t => t.TransactionType == type.Value);
        return query;
    }
}
