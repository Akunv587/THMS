using Microsoft.EntityFrameworkCore;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Repositories;

/// <summary>
/// 销售订单仓储实现
/// </summary>
public class SaleOrderRepository : ISaleOrderRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<SaleSaleOrder> _dbSet;

    public SaleOrderRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<SaleSaleOrder>();
    }

    public async Task<SaleSaleOrder?> GetByIdWithDetailsAsync(long id)
    {
        return await _dbSet
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .Include(o => o.Details)
                .ThenInclude(d => d.Warehouse)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<SaleSaleOrder?> GetByIdAsync(long id)
    {
        return await _dbSet.FindAsync(id);
    }

    public void Update(SaleSaleOrder order)
    {
        _dbSet.Update(order);
    }
}
