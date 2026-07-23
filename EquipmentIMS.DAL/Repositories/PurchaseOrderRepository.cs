using Microsoft.EntityFrameworkCore;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Repositories;

/// <summary>
/// 采购订单仓储实现
/// </summary>
public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<PurPurchaseOrder> _dbSet;

    public PurchaseOrderRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<PurPurchaseOrder>();
    }

    public async Task<PurPurchaseOrder?> GetByIdWithDetailsAsync(long id)
    {
        return await _dbSet
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .Include(o => o.Supplier)
            .Include(o => o.Warehouse)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<PurPurchaseOrder?> GetByIdAsync(long id)
    {
        return await _dbSet.FindAsync(id);
    }

    public void Update(PurPurchaseOrder order)
    {
        _dbSet.Update(order);
    }
}
