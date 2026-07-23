using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public DashboardService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<DashboardData> GetDashboardDataAsync(long userId, bool isManager)
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var totalStockValue = await _context.BaseProductStocks
            .Where(s => !s.IsDeleted)
            .Include(s => s.Product)
            .SumAsync(s => s.Quantity * s.Product.SalePrice);

        var monthlySales = await _context.InvStockTransactions
            .Where(t => t.TransactionType == TransactionType.SaleOut && t.CreateTime >= monthStart)
            .Include(t => t.Product)
            .SumAsync(t => t.Qty * t.Product.SalePrice);

        var monthlyPurchase = await _context.InvStockTransactions
            .Where(t => t.TransactionType == TransactionType.PurchaseIn && t.CreateTime >= monthStart)
            .Include(t => t.Product)
            .SumAsync(t => t.Qty * t.Product.PurchasePrice);

        var pendingWorkOrders = await _context.MaintRepairOrders
            .CountAsync(r => !r.IsDeleted && r.Status == RepairOrderStatus.PendingDispatch);

        var lowStockAlerts = await _context.InvStockAlerts
            .CountAsync(a => !a.IsResolved);

        var purchasePending = await _unitOfWork.GetRepository<PurPurchaseOrder>()
            .CountAsync(o => !o.IsDeleted && o.Status == PurchaseOrderStatus.PendingApproval);
        var salePending = await _unitOfWork.GetRepository<SaleSaleOrder>()
            .CountAsync(o => !o.IsDeleted && o.Status == SaleOrderStatus.PendingApproval);
        var transferPending = await _unitOfWork.GetRepository<InvStockTransfer>()
            .CountAsync(o => !o.IsDeleted && o.Status == TransferStatus.PendingApproval);
        var checkPending = await _unitOfWork.GetRepository<InvStockCheck>()
            .CountAsync(o => !o.IsDeleted && o.Status == StockCheckStatus.PendingApproval);

        return new DashboardData
        {
            StockTotalValue = totalStockValue,
            MonthlySalesAmount = monthlySales,
            MonthlyPurchaseAmount = monthlyPurchase,
            PendingWorkOrders = pendingWorkOrders,
            LowStockAlertCount = lowStockAlerts,
            PendingApprovalCount = purchasePending + salePending + transferPending + checkPending
        };
    }

    public async Task<SaleTrendData> GetSaleTrendAsync()
    {
        var result = new SaleTrendData();
        var now = DateTime.Now;

        for (int i = 5; i >= 0; i--)
        {
            var month = now.AddMonths(-i);
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var amount = await _context.InvStockTransactions
                .Where(t => t.TransactionType == TransactionType.SaleOut
                    && t.CreateTime >= monthStart && t.CreateTime < monthEnd)
                .Include(t => t.Product)
                .SumAsync(t => t.Qty * t.Product.SalePrice);

            result.Months.Add($"{monthStart.Year}-{monthStart.Month:D2}");
            result.Amounts.Add(amount);
        }

        return result;
    }

    public async Task<StockDistData> GetStockDistAsync()
    {
        var result = new StockDistData();

        var dist = await _context.BaseProductStocks
            .Where(s => !s.IsDeleted)
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .GroupBy(s => s.Product.Category.Name)
            .Select(g => new { Category = g.Key, Value = g.Sum(s => s.Quantity * s.Product.SalePrice) })
            .ToListAsync();

        foreach (var d in dist)
        {
            result.Categories.Add(d.Category);
            result.Values.Add(d.Value);
        }

        return result;
    }

    public async Task<List<PendingApprovalItem>> GetPendingApprovalsAsync()
    {
        var items = new List<PendingApprovalItem>();

        var purchaseOrders = await _context.PurPurchaseOrders
            .Where(o => !o.IsDeleted && o.Status == PurchaseOrderStatus.PendingApproval)
            .OrderByDescending(o => o.CreateTime)
            .Take(10)
            .Select(o => new PendingApprovalItem
            {
                OrderNo = o.OrderNo,
                TypeName = "采购订单",
                Amount = o.TotalAmount,
                Creator = o.CreateBy,
                OrderId = o.Id,
                OrderType = "purchase"
            })
            .ToListAsync();
        items.AddRange(purchaseOrders);

        var saleOrders = await _context.SaleSaleOrders
            .Where(o => !o.IsDeleted && o.Status == SaleOrderStatus.PendingApproval)
            .OrderByDescending(o => o.CreateTime)
            .Take(10)
            .Select(o => new PendingApprovalItem
            {
                OrderNo = o.OrderNo,
                TypeName = "销售订单",
                Amount = o.TotalAmount,
                Creator = o.CreateBy,
                OrderId = o.Id,
                OrderType = "sale"
            })
            .ToListAsync();
        items.AddRange(saleOrders);

        var transfers = await _context.InvStockTransfers
            .Where(o => !o.IsDeleted && o.Status == TransferStatus.PendingApproval)
            .OrderByDescending(o => o.CreateTime)
            .Take(10)
            .Select(o => new PendingApprovalItem
            {
                OrderNo = o.TransferNo,
                TypeName = "调拨单",
                Amount = 0,
                Creator = o.CreateBy,
                OrderId = o.Id,
                OrderType = "transfer"
            })
            .ToListAsync();
        items.AddRange(transfers);

        var checks = await _context.InvStockChecks
            .Where(o => !o.IsDeleted && o.Status == StockCheckStatus.PendingApproval)
            .OrderByDescending(o => o.CreateTime)
            .Take(10)
            .Select(o => new PendingApprovalItem
            {
                OrderNo = o.CheckNo,
                TypeName = "盘点单",
                Amount = o.TotalDiffAmount,
                Creator = o.CreateBy,
                OrderId = o.Id,
                OrderType = "check"
            })
            .ToListAsync();
        items.AddRange(checks);

        return items;
    }
}
