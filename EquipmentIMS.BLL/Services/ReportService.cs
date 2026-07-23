using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.DTOs.Reports;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ==================== 库存报表 ====================

    public async Task<List<InventoryReportDto>> GetInventorySummaryAsync(long? warehouseId, long? categoryId)
    {
        var stockQuery = _uow.GetRepository<BaseProductStock>().Query().Where(s => !s.IsDeleted);
        var prodQuery = _uow.GetRepository<BaseProduct>().Query().Where(p => !p.IsDeleted);
        var catQuery = _uow.GetRepository<BaseCategory>().Query();
        var whQuery = _uow.GetRepository<BaseWarehouse>().Query();

        if (warehouseId.HasValue)
            stockQuery = stockQuery.Where(s => s.WarehouseId == warehouseId.Value);
        if (categoryId.HasValue)
            prodQuery = prodQuery.Where(p => p.CategoryId == categoryId.Value);

        var result = await (from ps in stockQuery
                            join p in prodQuery on ps.ProductId equals p.Id
                            join c in catQuery on p.CategoryId equals c.Id
                            join w in whQuery on ps.WarehouseId equals w.Id
                            orderby p.Code
                            select new InventoryReportDto
                            {
                                ProductId = p.Id,
                                ProductCode = p.Code,
                                ProductName = p.Name,
                                Spec = p.Spec,
                                Brand = p.Brand,
                                CategoryName = c.Name,
                                WarehouseId = w.Id,
                                WarehouseName = w.Name,
                                Quantity = ps.Quantity,
                                LockQty = ps.LockQty,
                                AvailableQty = ps.Quantity - ps.LockQty,
                                MinStock = p.MinStock,
                                UnitPrice = p.PurchasePrice,
                                TotalValue = ps.Quantity * p.PurchasePrice,
                                IsWarning = (ps.Quantity - ps.LockQty) < p.MinStock
                            }).ToListAsync();

        return result;
    }

    public async Task<List<StockFundDto>> GetStockFundAnalysisAsync()
    {
        var query = from ps in _uow.GetRepository<BaseProductStock>().Query().Where(s => !s.IsDeleted)
                    join p in _uow.GetRepository<BaseProduct>().Query().Where(p => !p.IsDeleted) on ps.ProductId equals p.Id
                    join w in _uow.GetRepository<BaseWarehouse>().Query() on ps.WarehouseId equals w.Id
                    group new { ps, p, w } by new { w.Id, w.Name } into g
                    let totalVal = g.Sum(x => x.ps.Quantity * x.p.PurchasePrice)
                    orderby totalVal descending
                    select new StockFundDto
                    {
                        GroupName = g.Key.Name,
                        Dimension = "warehouse",
                        StockValue = totalVal,
                        ProductCount = g.Select(x => x.p.Id).Distinct().Count(),
                        TotalQty = g.Sum(x => x.ps.Quantity),
                        Percent = 0
                    };

        var items = await query.ToListAsync();
        var grandTotal = items.Sum(i => i.StockValue);
        if (grandTotal > 0)
            items.ForEach(i => i.Percent = Math.Round(i.StockValue / grandTotal * 100, 1));

        return items;
    }

    public async Task<PagedResult<StockTransactionDto>> GetStockTransactionsAsync(StockTransactionQueryDto query)
    {
        var repo = _uow.StockTransactions;
        var total = await repo.CountAsync(query.StartTime, query.EndTime, query.ProductId, query.WarehouseId,
            query.TransactionType.HasValue ? (TransactionType?)query.TransactionType.Value : null);

        var entities = (await repo.QueryAsync(query.StartTime, query.EndTime, query.ProductId, query.WarehouseId,
            query.TransactionType.HasValue ? (TransactionType?)query.TransactionType.Value : null,
            query.Page, query.PageSize)).ToList();

        var data = entities.Select(MapTransactionToDto).ToList();

        return new PagedResult<StockTransactionDto>
        {
            Items = data,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<List<StockWarningDto>> GetStockWarningsAsync()
    {
        var result = await (from ps in _uow.GetRepository<BaseProductStock>().Query().Where(s => !s.IsDeleted)
                            join p in _uow.GetRepository<BaseProduct>().Query().Where(p => !p.IsDeleted) on ps.ProductId equals p.Id
                            join w in _uow.GetRepository<BaseWarehouse>().Query() on ps.WarehouseId equals w.Id
                            where (ps.Quantity - ps.LockQty) < p.MinStock
                            orderby (ps.Quantity - ps.LockQty) - p.MinStock
                            select new StockWarningDto
                            {
                                ProductId = p.Id,
                                ProductCode = p.Code,
                                ProductName = p.Name,
                                Spec = p.Spec,
                                WarehouseName = w.Name,
                                CurrentQty = ps.Quantity - ps.LockQty,
                                MinStock = p.MinStock,
                                Gaps = p.MinStock - (ps.Quantity - ps.LockQty),
                                AlertLevel = (ps.Quantity - ps.LockQty) <= 0 ? "严重不足" : "低于安全库存",
                                CreateTime = p.UpdateTime ?? p.CreateTime
                            }).ToListAsync();

        return result;
    }

    // ==================== 销售报表 ====================

    public async Task<List<SaleRankingDto>> GetSaleRankingAsync(DateTime startDate, DateTime endDate, string rankBy)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);
        var saleOutQuery = _uow.GetRepository<SaleSaleOut>().Query().Where(o => !o.IsDeleted && o.OutTime >= startDate && o.OutTime <= endDateEnd);

        return rankBy switch
        {
            "product" => await GetProductSaleRankingAsync(saleOutQuery),
            "customer" => await GetCustomerSaleRankingAsync(saleOutQuery),
            "salesman" => await GetSalesmanSaleRankingAsync(saleOutQuery),
            _ => await GetProductSaleRankingAsync(saleOutQuery)
        };
    }

    private async Task<List<SaleRankingDto>> GetProductSaleRankingAsync(IQueryable<SaleSaleOut> saleOutQuery)
    {
        var result = await (from d in _uow.GetRepository<SaleSaleOutDetail>().Query()
                            join o in saleOutQuery on d.SaleOutId equals o.Id
                            join p in _uow.GetRepository<BaseProduct>().Query() on d.ProductId equals p.Id
                            group new { d, p } by new { p.Id, p.Name } into g
                            let totalAmt = g.Sum(x => x.d.Amount)
                            orderby totalAmt descending
                            select new SaleRankingDto
                            {
                                Name = g.Key.Name,
                                TotalQuantity = g.Sum(x => x.d.Qty),
                                TotalAmount = totalAmt,
                                OrderCount = g.Select(x => x.d.SaleOutId).Distinct().Count(),
                                Ranking = 0
                            }).ToListAsync();

        for (int i = 0; i < result.Count; i++) result[i].Ranking = i + 1;
        return result;
    }

    private async Task<List<SaleRankingDto>> GetCustomerSaleRankingAsync(IQueryable<SaleSaleOut> saleOutQuery)
    {
        var result = await (from o in saleOutQuery
                            join c in _uow.GetRepository<BaseCustomer>().Query() on o.CustomerId equals c.Id
                            group o by new { c.Id, c.Name } into g
                            let totalAmt = g.Sum(x => x.TotalAmount)
                            orderby totalAmt descending
                            select new SaleRankingDto
                            {
                                Name = g.Key.Name,
                                TotalQuantity = 0,
                                TotalAmount = totalAmt,
                                OrderCount = g.Count(),
                                Ranking = 0
                            }).ToListAsync();

        for (int i = 0; i < result.Count; i++) result[i].Ranking = i + 1;
        return result;
    }

    private async Task<List<SaleRankingDto>> GetSalesmanSaleRankingAsync(IQueryable<SaleSaleOut> saleOutQuery)
    {
        var result = await (from o in saleOutQuery
                            where o.Operator != null
                            group o by o.Operator! into g
                            let totalAmt = g.Sum(x => x.TotalAmount)
                            orderby totalAmt descending
                            select new SaleRankingDto
                            {
                                Name = g.Key,
                                TotalQuantity = 0,
                                TotalAmount = totalAmt,
                                OrderCount = g.Count(),
                                Ranking = 0
                            }).ToListAsync();

        for (int i = 0; i < result.Count; i++) result[i].Ranking = i + 1;
        return result;
    }

    public async Task<List<SaleTrendDto>> GetSaleTrendAsync(DateTime startDate, DateTime endDate, string groupBy)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);
        var query = _uow.GetRepository<SaleSaleOut>().Query()
            .Where(o => !o.IsDeleted && o.OutTime >= startDate && o.OutTime <= endDateEnd);

        if (groupBy == "quarter")
        {
            var result = await query.ToListAsync();
            return result.GroupBy(o => new { o.OutTime.Year, Quarter = (o.OutTime.Month - 1) / 3 + 1 })
                .Select(g => new SaleTrendDto
                {
                    Period = $"{g.Key.Year}Q{g.Key.Quarter}",
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count()
                }).OrderBy(t => t.Period).ToList();
        }

        // month (default)
        var monthly = await query.GroupBy(o => new { o.OutTime.Year, o.OutTime.Month })
            .Select(g => new SaleTrendDto
            {
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                TotalAmount = g.Sum(x => x.TotalAmount),
                OrderCount = g.Count()
            }).OrderBy(t => t.Period).ToListAsync();

        return monthly;
    }

    public async Task<SaleReturnStatDto> GetSaleReturnStatAsync(DateTime startDate, DateTime endDate)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);

        var returnQuery = _uow.GetRepository<SaleReturn>().Query()
            .Where(r => !r.IsDeleted && r.ReturnDate >= startDate && r.ReturnDate <= endDateEnd);
        var saleQuery = _uow.GetRepository<SaleSaleOut>().Query()
            .Where(o => !o.IsDeleted && o.OutTime >= startDate && o.OutTime <= endDateEnd);

        var returnAmount = await returnQuery.SumAsync(r => r.TotalAmount);
        var returnCount = await returnQuery.CountAsync();
        var saleAmount = await saleQuery.SumAsync(o => o.TotalAmount);
        var saleCount = await saleQuery.CountAsync();

        // Monthly trends
        var returns = await returnQuery.ToListAsync();
        var outs = await saleQuery.ToListAsync();

        var months = new HashSet<string>();
        for (var d = new DateTime(startDate.Year, startDate.Month, 1); d <= endDate; d = d.AddMonths(1))
            months.Add($"{d.Year}-{d.Month:D2}");

        var trends = months.Select(m =>
        {
            var rAmt = returns.Where(r => $"{r.ReturnDate.Year}-{r.ReturnDate.Month:D2}" == m).Sum(r => r.TotalAmount);
            var sAmt = outs.Where(o => $"{o.OutTime.Year}-{o.OutTime.Month:D2}" == m).Sum(o => o.TotalAmount);
            return new SaleReturnTrendDto
            {
                Month = m,
                SaleAmount = sAmt,
                ReturnAmount = rAmt,
                ReturnRate = sAmt > 0 ? Math.Round(rAmt / sAmt * 100, 2) : 0
            };
        }).OrderBy(t => t.Month).ToList();

        return new SaleReturnStatDto
        {
            ReturnCount = returnCount,
            ReturnAmount = returnAmount,
            SaleCount = saleCount,
            SaleAmount = saleAmount,
            ReturnRate = saleAmount > 0 ? Math.Round(returnAmount / saleAmount * 100, 2) : 0,
            MonthlyTrends = trends
        };
    }

    // ==================== 采购报表 ====================

    public async Task<List<PurchaseSummaryDto>> GetPurchaseSummaryAsync(DateTime startDate, DateTime endDate, string groupBy)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);
        var query = _uow.GetRepository<PurPurchaseIn>().Query()
            .Where(p => !p.IsDeleted && p.InDate >= startDate && p.InDate <= endDateEnd);

        if (groupBy == "quarter")
        {
            var all = await query.ToListAsync();
            return all.GroupBy(p => new { p.InDate.Year, Quarter = (p.InDate.Month - 1) / 3 + 1 })
                .Select(g => new PurchaseSummaryDto
                {
                    Period = $"{g.Key.Year}Q{g.Key.Quarter}",
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count(),
                    SupplierCount = g.Select(x => x.SupplierId).Distinct().Count()
                }).OrderBy(t => t.Period).ToList();
        }

        return await query.GroupBy(p => new { p.InDate.Year, p.InDate.Month })
            .Select(g => new PurchaseSummaryDto
            {
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                TotalAmount = g.Sum(x => x.TotalAmount),
                OrderCount = g.Count(),
                SupplierCount = g.Select(x => x.SupplierId).Distinct().Count()
            }).OrderBy(t => t.Period).ToListAsync();
    }

    public async Task<PurchaseReturnStatDto> GetPurchaseReturnStatAsync(DateTime startDate, DateTime endDate)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);

        var retQuery = _uow.GetRepository<PurPurchaseReturn>().Query()
            .Where(r => !r.IsDeleted && r.ReturnDate >= startDate && r.ReturnDate <= endDateEnd);
        var inQuery = _uow.GetRepository<PurPurchaseIn>().Query()
            .Where(p => !p.IsDeleted && p.InDate >= startDate && p.InDate <= endDateEnd);

        var retAmt = await retQuery.SumAsync(r => r.TotalAmount);
        var retCnt = await retQuery.CountAsync();
        var purAmt = await inQuery.SumAsync(p => p.TotalAmount);
        var purCnt = await inQuery.CountAsync();

        var returns = await retQuery.ToListAsync();
        var ins = await inQuery.ToListAsync();

        var months = new HashSet<string>();
        for (var d = new DateTime(startDate.Year, startDate.Month, 1); d <= endDate; d = d.AddMonths(1))
            months.Add($"{d.Year}-{d.Month:D2}");

        var trends = months.Select(m =>
        {
            var rAmt = returns.Where(r => $"{r.ReturnDate.Year}-{r.ReturnDate.Month:D2}" == m).Sum(r => r.TotalAmount);
            var pAmt = ins.Where(p => $"{p.InDate.Year}-{p.InDate.Month:D2}" == m).Sum(p => p.TotalAmount);
            return new PurchaseReturnTrendDto
            {
                Month = m,
                PurchaseAmount = pAmt,
                ReturnAmount = rAmt,
                ReturnRate = pAmt > 0 ? Math.Round(rAmt / pAmt * 100, 2) : 0
            };
        }).OrderBy(t => t.Month).ToList();

        return new PurchaseReturnStatDto
        {
            ReturnCount = retCnt,
            ReturnAmount = retAmt,
            PurchaseCount = purCnt,
            PurchaseAmount = purAmt,
            ReturnRate = purAmt > 0 ? Math.Round(retAmt / purAmt * 100, 2) : 0,
            MonthlyTrends = trends
        };
    }

    // ==================== 维修报表 ====================

    public async Task<List<RepairStatDto>> GetRepairStatAsync(DateTime startDate, DateTime endDate, string statBy)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);
        var query = _uow.GetRepository<MaintRepairOrder>().Query()
            .Where(r => !r.IsDeleted && r.CreateTime >= startDate && r.CreateTime <= endDateEnd);

        return statBy switch
        {
            "status" => await query.GroupBy(r => r.Status)
                .Select(g => new RepairStatDto
                {
                    GroupLabel = g.Key.ToString(),
                    StatBy = "status",
                    OrderCount = g.Count(),
                    TotalRepairCost = g.Sum(x => x.RepairCost),
                    TotalPartsCost = g.Sum(x => x.PartsCost),
                    TotalCost = g.Sum(x => x.TotalCost),
                    AvgHours = g.Average(x => (double)x.WorkHours)
                }).ToListAsync(),

            "technician" => await (from r in query
                                   join u in _uow.GetRepository<SysUser>().Query() on r.TechnicianId equals u.Id into uj
                                   from u in uj.DefaultIfEmpty()
                                   group r by r.TechnicianName ?? "未分配" into g
                                   select new RepairStatDto
                                   {
                                       GroupLabel = g.Key,
                                       StatBy = "technician",
                                       OrderCount = g.Count(),
                                       TotalRepairCost = g.Sum(x => x.RepairCost),
                                       TotalPartsCost = g.Sum(x => x.PartsCost),
                                       TotalCost = g.Sum(x => x.TotalCost),
                                       AvgHours = g.Average(x => (double)x.WorkHours)
                                   }).ToListAsync(),

            "customer" => await (from r in query
                                 join c in _uow.GetRepository<BaseCustomer>().Query() on r.CustomerId equals c.Id
                                 group r by c.Name into g
                                 select new RepairStatDto
                                 {
                                     GroupLabel = g.Key,
                                     StatBy = "customer",
                                     OrderCount = g.Count(),
                                     TotalRepairCost = g.Sum(x => x.RepairCost),
                                     TotalPartsCost = g.Sum(x => x.PartsCost),
                                     TotalCost = g.Sum(x => x.TotalCost),
                                     AvgHours = g.Average(x => (double)x.WorkHours)
                                 }).ToListAsync(),

            _ => new List<RepairStatDto>()
        };
    }

    public async Task<RepairCostAnalysisDto> GetRepairCostAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);
        var orders = await _uow.GetRepository<MaintRepairOrder>().Query()
            .Include(r => r.DeviceArchive).ThenInclude(d => d.Product)
            .Include(r => r.Customer)
            .Where(r => !r.IsDeleted && r.CreateTime >= startDate && r.CreateTime <= endDateEnd)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();

        var totalOrders = orders.Count;
        if (totalOrders == 0)
            return new RepairCostAnalysisDto();

        var details = orders.Select(r => new RepairCostDetailDto
        {
            OrderNo = r.OrderNo,
            SerialNo = r.DeviceArchive?.SerialNo,
            ProductName = r.DeviceArchive?.Product?.Name,
            CustomerName = r.Customer?.Name,
            TechnicianName = r.TechnicianName,
            StatusName = GetStatusName(r.Status),
            RepairCost = r.RepairCost,
            PartsCost = r.PartsCost,
            TotalCost = r.TotalCost,
            WorkHours = r.WorkHours,
            IsWarranty = r.IsWarranty,
            CreateTime = r.CreateTime
        }).ToList();

        return new RepairCostAnalysisDto
        {
            TotalOrders = totalOrders,
            TotalRepairCost = orders.Sum(r => r.RepairCost),
            TotalPartsCost = orders.Sum(r => r.PartsCost),
            TotalCost = orders.Sum(r => r.TotalCost),
            TotalWorkHours = (double)orders.Sum(r => r.WorkHours),
            AvgWorkHours = Math.Round(orders.Average(r => (double)r.WorkHours), 2),
            WarrantyOrders = orders.Count(r => r.IsWarranty),
            WarrantyRate = (decimal)Math.Round((double)orders.Count(r => r.IsWarranty) / totalOrders * 100, 2),
            Details = details
        };
    }

    public async Task<decimal> GetWarrantyRepairRateAsync(DateTime startDate, DateTime endDate)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);
        var total = await _uow.GetRepository<MaintRepairOrder>().Query()
            .CountAsync(r => !r.IsDeleted && r.CreateTime >= startDate && r.CreateTime <= endDateEnd);
        if (total == 0) return 0;

        var warranty = await _uow.GetRepository<MaintRepairOrder>().Query()
            .CountAsync(r => !r.IsDeleted && r.CreateTime >= startDate && r.CreateTime <= endDateEnd && r.IsWarranty);

        return Math.Round((decimal)warranty / total * 100, 2);
    }

    public async Task<List<PartsUsageDto>> GetPartsUsageStatAsync(DateTime startDate, DateTime endDate)
    {
        var endDateEnd = endDate.Date.AddDays(1).AddSeconds(-1);

        var result = await (from rp in _uow.GetRepository<MaintRepairPart>().Query().Where(r => !r.IsDeleted)
                            join ro in _uow.GetRepository<MaintRepairOrder>().Query() on rp.RepairOrderId equals ro.Id
                            join p in _uow.GetRepository<BaseProduct>().Query() on rp.ProductId equals p.Id
                            where ro.CreateTime >= startDate && ro.CreateTime <= endDateEnd
                            group rp by new { p.Id, p.Code, p.Name } into g
                            let totalAmt = g.Sum(x => x.Amount)
                            let totalQty = g.Sum(x => x.Qty)
                            orderby totalAmt descending
                            select new PartsUsageDto
                            {
                                ProductId = g.Key.Id,
                                ProductCode = g.Key.Code,
                                ProductName = g.Key.Name,
                                UsageCount = g.Select(x => x.RepairOrderId).Distinct().Count(),
                                TotalQty = totalQty,
                                TotalAmount = totalAmt,
                                AvgUnitPrice = totalQty > 0 ? Math.Round(totalAmt / totalQty, 2) : 0
                            }).ToListAsync();

        return result;
    }

    private static StockTransactionDto MapTransactionToDto(InvStockTransaction t) => new()
    {
        Id = t.Id,
        TransactionNo = t.TransactionNo,
        TransactionType = (int)t.TransactionType,
        TransactionTypeName = t.TransactionType.ToString(),
        ProductId = t.ProductId,
        ProductCode = "",
        ProductName = "",
        WarehouseId = t.WarehouseId,
        WarehouseName = "",
        Qty = t.Qty,
        BalanceQty = t.BalanceQty,
        RefType = t.RefType,
        RefId = t.RefId,
        RefNo = t.RefNo,
        Operator = t.Operator,
        Remark = t.Remark,
        CreateTime = t.CreateTime
    };

    private static string GetStatusName(RepairOrderStatus s) => s switch
    {
        RepairOrderStatus.PendingDispatch => "待派工",
        RepairOrderStatus.Dispatched => "已派工",
        RepairOrderStatus.InRepair => "维修中",
        RepairOrderStatus.PendingConfirm => "待确认",
        RepairOrderStatus.Completed => "已完工",
        RepairOrderStatus.Closed => "已关闭",
        RepairOrderStatus.Cancelled => "已取消",
        _ => s.ToString()
    };
}
