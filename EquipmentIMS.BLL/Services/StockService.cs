using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;

namespace EquipmentIMS.BLL.Services;

public class StockService : IStockService
{
    private readonly IUnitOfWork _uow;
    private readonly INumberGeneratorService _numberGen;
    private readonly IMemoryCache _cache;
    private readonly IExportTaskQueue _exportQueue;
    private readonly AppDbContext _db;
    private readonly INotificationService _notification;
    private readonly ILogger<StockService> _logger;
    private readonly ConcurrentDictionary<string, byte> _stockCacheKeys = new();

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string StockListCacheKey = "StockList";

    public StockService(IUnitOfWork uow, INumberGeneratorService numberGen, IMemoryCache cache, IExportTaskQueue exportQueue,
        AppDbContext db, INotificationService notification, ILogger<StockService> logger)
    {
        _uow = uow;
        _numberGen = numberGen;
        _cache = cache;
        _exportQueue = exportQueue;
        _db = db;
        _notification = notification;
        _logger = logger;
    }

    public async Task<PagedResult<StockQueryDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var q = _uow.GetRepository<BaseProductStock>().Query()
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .Include(s => s.Warehouse)
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(s => s.Product.Code.Contains(keyword) || s.Product.Name.Contains(keyword));

        var total = await q.CountAsync();
        var items = await q.OrderBy(s => s.Product.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<StockQueryDto>
        {
            Items = items.Select(s => new StockQueryDto
            {
                ProductId = s.ProductId,
                ProductCode = s.Product?.Code ?? "",
                ProductName = s.Product?.Name ?? "",
                Spec = s.Product?.Spec,
                Brand = s.Product?.Brand,
                CategoryName = s.Product?.Category?.Name ?? "",
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse?.Name ?? "",
                Quantity = s.Quantity,
                LockQty = s.LockQty,
                AvailableQty = s.Quantity - s.LockQty,
                MinStock = s.Product?.MinStock ?? 0,
                WarningLevel = s.Product != null && s.Product.MinStock > 0 && s.Quantity <= s.Product.MinStock ? "低库存" : "正常"
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ==================== 库存查询（含缓存） ====================

    public async Task<PagedResult<StockListDto>> GetStockListAsync(StockQueryDto query)
    {
        var cacheKey = $"{StockListCacheKey}_{query.Keyword}_{query.CategoryId}_{query.WarehouseId}_{query.WarningOnly}_{query.Page}_{query.PageSize}";
        _stockCacheKeys.TryAdd(cacheKey, 0);
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await QueryStockListFromDbAsync(query);
            }) ?? new PagedResult<StockListDto>();
    }

    public void InvalidateStockCache()
    {
        foreach (var key in _stockCacheKeys.Keys)
            _cache.Remove(key);
        _stockCacheKeys.Clear();
    }

    private async Task<PagedResult<StockListDto>> QueryStockListFromDbAsync(StockQueryDto query)
    {
        var q = _uow.GetRepository<BaseProductStock>().Query()
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .Include(s => s.Warehouse)
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(s => s.Product.Code.Contains(query.Keyword) || s.Product.Name.Contains(query.Keyword));
        if (query.CategoryId.HasValue)
            q = q.Where(s => s.Product.CategoryId == query.CategoryId.Value);
        if (query.WarehouseId.HasValue)
            q = q.Where(s => s.WarehouseId == query.WarehouseId.Value);
        if (query.WarningOnly == true)
            q = q.Where(s => s.Product.MinStock > 0 && s.Quantity <= s.Product.MinStock);

        var total = await q.CountAsync();
        var items = await q.OrderBy(s => s.Product.Code)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<StockListDto>
        {
            Items = items.Select(s => new StockListDto
            {
                ProductId = s.ProductId,
                ProductCode = s.Product?.Code ?? "",
                ProductName = s.Product?.Name ?? "",
                Spec = s.Product?.Spec,
                Brand = s.Product?.Brand,
                CategoryId = s.Product?.CategoryId ?? 0,
                CategoryName = s.Product?.Category?.Name ?? "",
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse?.Name ?? "",
                Quantity = s.Quantity,
                LockQty = s.LockQty,
                AvailableQty = s.Quantity - s.LockQty,
                MinStock = s.Product?.MinStock ?? 0,
                WarningLevel = s.Product != null && s.Product.MinStock > 0 && s.Quantity <= s.Product.MinStock ? "低库存" : "正常"
            }).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // ==================== 库存流水 ====================

    public async Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(StockTransactionQueryDto query)
    {
        var repo = _uow.StockTransactions;

        // 关键字搜索：先解析为产品ID列表
        List<long>? keywordProductIds = null;
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            keywordProductIds = await _uow.GetRepository<BaseProduct>().Query()
                .Where(p => p.Code.Contains(query.Keyword) || p.Name.Contains(query.Keyword))
                .Select(p => p.Id).ToListAsync();
            if (keywordProductIds.Count == 0)
                return new PagedResult<StockTransactionDto> { Page = query.Page, PageSize = query.PageSize };
        }

        // 合并 productId 和 keyword 结果
        long? effectiveProductId = query.ProductId;
        if (keywordProductIds is { Count: 1 } && !query.ProductId.HasValue)
            effectiveProductId = keywordProductIds[0];

        var total = await repo.CountAsync(query.StartTime, query.EndTime, effectiveProductId, query.WarehouseId,
            query.TransactionType.HasValue ? (TransactionType?)query.TransactionType.Value : null);

        var list = (await repo.QueryAsync(query.StartTime, query.EndTime, effectiveProductId, query.WarehouseId,
            query.TransactionType.HasValue ? (TransactionType?)query.TransactionType.Value : null,
            1, total)).ToList();

        // 如果 keyword 匹配多个产品，在内存中过滤
        if (keywordProductIds is { Count: > 1 } || (keywordProductIds is { Count: 1 } && query.ProductId.HasValue))
        {
            var ids = keywordProductIds!.ToHashSet();
            list = list.Where(t => ids.Contains(t.ProductId)).ToList();
            total = list.Count;
        }

        var paged = list.OrderByDescending(t => t.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var productIds = paged.Select(t => t.ProductId).Distinct().ToList();
        var warehouseIds = paged.Select(t => t.WarehouseId).Distinct().ToList();
        var products = await _uow.GetRepository<BaseProduct>().Query()
            .Where(p => productIds.Contains(p.Id)).ToListAsync();
        var warehouses = await _uow.GetRepository<BaseWarehouse>().Query()
            .Where(w => warehouseIds.Contains(w.Id)).ToListAsync();

        return new PagedResult<StockTransactionDto>
        {
            Items = paged.Select(t =>
            {
                var p = products.FirstOrDefault(x => x.Id == t.ProductId);
                var w = warehouses.FirstOrDefault(x => x.Id == t.WarehouseId);
                return new StockTransactionDto
                {
                    Id = t.Id,
                    TransactionNo = t.TransactionNo,
                    TransactionType = (int)t.TransactionType,
                    TransactionTypeName = GetTransactionTypeName(t.TransactionType),
                    ProductId = t.ProductId,
                    ProductCode = p?.Code ?? "",
                    ProductName = p?.Name ?? "",
                    WarehouseId = t.WarehouseId,
                    WarehouseName = w?.Name ?? "",
                    Qty = t.Qty,
                    BalanceQty = t.BalanceQty,
                    RefType = t.RefType,
                    RefId = t.RefId,
                    RefNo = t.RefNo,
                    Operator = t.Operator,
                    Remark = t.Remark,
                    CreateTime = t.CreateTime
                };
            }).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // ==================== 导出 ====================

    public async Task<byte[]> ExportTransactionsAsync(StockTransactionQueryDto query)
    {
        var repo = _uow.StockTransactions;

        // 关键字搜索：解析为产品ID列表
        List<long>? keywordProductIds = null;
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            keywordProductIds = await _uow.GetRepository<BaseProduct>().Query()
                .Where(p => p.Code.Contains(query.Keyword) || p.Name.Contains(query.Keyword))
                .Select(p => p.Id).ToListAsync();
            if (keywordProductIds.Count == 0)
                return BuildExcel(new(), new(), new());
        }

        long? effectiveProductId = query.ProductId;
        if (keywordProductIds is { Count: 1 } && !query.ProductId.HasValue)
            effectiveProductId = keywordProductIds[0];

        var total = await repo.CountAsync(query.StartTime, query.EndTime, effectiveProductId, query.WarehouseId,
            query.TransactionType.HasValue ? (TransactionType?)query.TransactionType.Value : null);

        // 超过 5000 条拒绝同步导出
        if (total > 5000)
            throw new BusinessException($"数据量过大（{total}条），请使用异步导出", "EXPORT_TOO_LARGE");

        var data = (await repo.QueryAsync(query.StartTime, query.EndTime, effectiveProductId, query.WarehouseId,
            query.TransactionType.HasValue ? (TransactionType?)query.TransactionType.Value : null,
            1, total + 1)).ToList();

        // 如果 keyword 匹配多个产品，在内存中过滤
        if (keywordProductIds is { Count: > 1 } || (keywordProductIds is { Count: 1 } && query.ProductId.HasValue))
        {
            var ids = keywordProductIds!.ToHashSet();
            data = data.Where(t => ids.Contains(t.ProductId)).ToList();
        }

        var productIds = data.Select(t => t.ProductId).Distinct().ToList();
        var warehouseIds = data.Select(t => t.WarehouseId).Distinct().ToList();
        var products = await _uow.GetRepository<BaseProduct>().Query()
            .Where(p => productIds.Contains(p.Id)).ToListAsync();
        var warehouses = await _uow.GetRepository<BaseWarehouse>().Query()
            .Where(w => warehouseIds.Contains(w.Id)).ToListAsync();

        return BuildExcel(data, products, warehouses);
    }

    public async Task<Guid> StartExportTaskAsync(StockTransactionQueryDto query, long userId)
    {
        var taskId = Guid.NewGuid();
        await _exportQueue.EnqueueAsync(new ExportTaskItem
        {
            TaskId = taskId,
            Query = query,
            UserId = userId
        });
        return taskId;
    }

    public async Task<string?> GetExportFilePathAsync(Guid taskId)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports");
        if (!Directory.Exists(dir)) return null;
        var files = Directory.GetFiles(dir, $"{taskId}_*.xlsx");
        return files.Length > 0 ? files[0] : null;
    }

    private byte[] BuildExcel(List<InvStockTransaction> data, List<BaseProduct> products, List<BaseWarehouse> warehouses)
    {
        using var stream = new MemoryStream();
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("库存流水");

        var styleHeader = workbook.CreateCellStyle();
        var fontHeader = workbook.CreateFont();
        fontHeader.IsBold = true;
        styleHeader.SetFont(fontHeader);

        var headers = new[] { "流水号", "时间", "类型", "设备编码", "设备名称", "仓库", "数量", "变动后库存", "关联单据", "操作人", "备注" };
        var headerRow = sheet.CreateRow(0);
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = styleHeader;
        }

        for (int i = 0; i < data.Count; i++)
        {
            var t = data[i];
            var row = sheet.CreateRow(i + 1);
            row.CreateCell(0).SetCellValue(t.TransactionNo);
            row.CreateCell(1).SetCellValue(t.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            row.CreateCell(2).SetCellValue(GetTransactionTypeName(t.TransactionType));
            row.CreateCell(3).SetCellValue(products.FirstOrDefault(p => p.Id == t.ProductId)?.Code ?? "");
            row.CreateCell(4).SetCellValue(products.FirstOrDefault(p => p.Id == t.ProductId)?.Name ?? "");
            row.CreateCell(5).SetCellValue(warehouses.FirstOrDefault(w => w.Id == t.WarehouseId)?.Name ?? "");
            row.CreateCell(6).SetCellValue(t.Qty);
            row.CreateCell(7).SetCellValue(t.BalanceQty);
            row.CreateCell(8).SetCellValue(t.RefNo ?? "");
            row.CreateCell(9).SetCellValue(t.Operator);
            row.CreateCell(10).SetCellValue(t.Remark ?? "");
        }

        for (int i = 0; i < headers.Length; i++)
            sheet.SetColumnWidth(i, 5000);

        workbook.Write(stream, leaveOpen: false);
        return stream.ToArray();
    }

    public async Task<List<CategorySimpleDto>> GetCategoriesAsync()
    {
        return await _uow.GetRepository<BaseCategory>().Query()
            .Where(c => !c.IsDeleted)
            .Select(c => new CategorySimpleDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<List<WarehouseSimpleDto>> GetWarehousesAsync()
    {
        return await _uow.GetRepository<BaseWarehouse>().Query()
            .Where(w => !w.IsDeleted)
            .Select(w => new WarehouseSimpleDto { Id = w.Id, Name = w.Name })
            .ToListAsync();
    }

    // ==================== 流水类型映射 ====================

    private static string GetTransactionTypeName(TransactionType type) => type switch
    {
        TransactionType.PurchaseIn => "采购入库",
        TransactionType.PurchaseReturnOut => "采购退货出库",
        TransactionType.SaleOut => "销售出库",
        TransactionType.SaleReturnIn => "销售退货入库",
        TransactionType.TransferIn => "调拨入库",
        TransactionType.TransferOut => "调拨出库",
        TransactionType.StockGain => "盘盈入库",
        TransactionType.StockLoss => "盘亏出库",
        TransactionType.InitialImport => "初始导入",
        TransactionType.RepairUse => "维修领用",
        _ => type.ToString()
    };

    // ==================== 期初导入（已存在） ====================

    public async Task<InitialImportPreviewDto> PreviewAsync(IFormFile file)
    {
        var result = new InitialImportPreviewDto();

        var productRepo = _uow.GetRepository<BaseProduct>();
        var warehouseRepo = _uow.GetRepository<BaseWarehouse>();
        var stockRepo = _uow.GetRepository<BaseProductStock>();

        var products = await productRepo.Query().Where(p => !p.IsDeleted).ToListAsync();
        var warehouses = await warehouseRepo.Query().Where(w => !w.IsDeleted).ToListAsync();
        var stocks = await stockRepo.Query().Where(s => !s.IsDeleted && s.Quantity > 0).ToListAsync();

        using var stream = file.OpenReadStream();
        var workbook = new XSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);

        for (int row = 1; row <= sheet.LastRowNum; row++)
        {
            var r = sheet.GetRow(row);
            if (r == null) continue;
            result.TotalCount++;

            var productCode = GetCellString(r, 0);
            var warehouseName = GetCellString(r, 1);
            var qty = (int)GetCellDecimal(r, 2);

            var item = new InitialImportPreviewItemDto { Row = row + 1, ProductCode = productCode ?? "", WarehouseName = warehouseName ?? "", Quantity = qty };

            if (string.IsNullOrWhiteSpace(productCode))
            {
                item.Error = "设备编码不能为空";
                result.InvalidCount++;
                result.Items.Add(item);
                continue;
            }
            if (string.IsNullOrWhiteSpace(warehouseName))
            {
                item.Error = "仓库名称不能为空";
                result.InvalidCount++;
                result.Items.Add(item);
                continue;
            }
            if (qty <= 0)
            {
                item.Error = "数量必须大于0";
                result.InvalidCount++;
                result.Items.Add(item);
                continue;
            }

            var product = products.FirstOrDefault(p => p.Code == productCode);
            if (product == null)
            {
                item.Error = $"设备编码'{productCode}'不存在";
                result.InvalidCount++;
                result.Items.Add(item);
                continue;
            }

            var warehouse = warehouses.FirstOrDefault(w => w.Name == warehouseName);
            if (warehouse == null)
            {
                item.Error = $"仓库'{warehouseName}'不存在";
                result.InvalidCount++;
                result.Items.Add(item);
                continue;
            }

            if (stocks.Any(s => s.ProductId == product.Id && s.WarehouseId == warehouse.Id))
            {
                item.Error = $"设备'{productCode}'在仓库'{warehouseName}'已有库存，不允许重复导入";
                result.InvalidCount++;
                result.Items.Add(item);
                continue;
            }

            item.ProductId = product.Id;
            item.ProductName = product.Name;
            item.WarehouseId = warehouse.Id;
            item.Valid = true;
            result.ValidCount++;
            result.Items.Add(item);
        }

        return result;
    }

    public async Task<InitialImportResultDto> ImportAsync(List<InitialStockDto> items, string currentUser)
    {
        var result = new InitialImportResultDto();
        var batchNo = $"INIT-{DateTime.Now:yyyyMMdd}";

        await using var tx = await _uow.BeginTransactionAsync();
        try
        {
            var stockRepo = _uow.GetRepository<BaseProductStock>();

            foreach (var item in items)
            {
                var key = $"{item.ProductCode} / {item.WarehouseName}";

                try
                {
                    var existing = await _uow.Stocks.FindByProductAndWarehouseAsync(item.ProductId, item.WarehouseId);
                    if (existing != null)
                        throw new BusinessException($"设备'{item.ProductCode}'在仓库已有库存，不允许重复导入", "STOCK_EXISTS");

                    var stock = new BaseProductStock
                    {
                        ProductId = item.ProductId,
                        WarehouseId = item.WarehouseId,
                        Quantity = item.Quantity,
                        LockQty = 0,
                        CreateBy = currentUser,
                        CreateTime = DateTime.Now
                    };
                    await stockRepo.AddAsync(stock);
                    await _uow.SaveChangesAsync();

                    await _uow.StockTransactions.AddAsync(new InvStockTransaction
                    {
                        TransactionNo = await _numberGen.GenerateWithTimeAsync("TXN", "Inv_StockTransaction", "TransactionNo"),
                        TransactionType = TransactionType.InitialImport,
                        ProductId = item.ProductId,
                        WarehouseId = item.WarehouseId,
                        Qty = item.Quantity,
                        BalanceQty = item.Quantity,
                        RefType = "InitialImport",
                        RefNo = batchNo,
                        Operator = currentUser,
                        Remark = $"期初库存导入 {batchNo}",
                        CreateBy = currentUser,
                        CreateTime = DateTime.Now
                    });

                    result.SuccessCount++;
                }
                catch (BusinessException ex)
                {
                    result.FailCount++;
                    result.FailItems.Add($"{key}: {ex.Message}");
                }
            }

            // 部分失败时全部回滚
            if (result.FailCount > 0)
            {
                await tx.RollbackAsync();
                result.SuccessCount = 0;
                result.Message = $"导入失败，{result.FailCount}条数据校验不通过";
                return result;
            }

            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
            InvalidateStockCache();
            result.Message = $"期初库存导入成功，共{result.SuccessCount}条记录（批次号：{batchNo}）";
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new BusinessException("库存数据已被他人修改，请刷新后重试", "STOCK_CONFLICT");
        }

        return result;
    }

    private static string? GetCellString(IRow row, int col)
    {
        var cell = row.GetCell(col);
        if (cell == null) return null;
        return cell.CellType switch
        {
            CellType.Numeric => cell.NumericCellValue.ToString(),
            CellType.String => cell.StringCellValue.Trim(),
            _ => cell.ToString()?.Trim()
        };
    }

    private static decimal GetCellDecimal(IRow row, int col)
    {
        var cell = row.GetCell(col);
        if (cell == null) return 0;
        if (cell.CellType == CellType.Numeric) return (decimal)cell.NumericCellValue;
        return decimal.TryParse(cell.ToString(), out var v) ? v : 0;
    }

    // ==================== 盘点管理 ====================

    public async Task<PagedResult<StockCheckListDto>> GetCheckListAsync(StockCheckQueryDto query)
    {
        var checkRepo = _uow.GetRepository<InvStockCheck>();
        var q = checkRepo.Query()
            .Include(c => c.Warehouse)
            .Include(c => c.Details)
            .Where(c => !c.IsDeleted);

        if (query.Status.HasValue)
            q = q.Where(c => c.Status == (StockCheckStatus)query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(c => c.CheckNo.Contains(query.Keyword));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<StockCheckListDto>
        {
            Items = items.Select(c => new StockCheckListDto
            {
                Id = c.Id,
                CheckNo = c.CheckNo,
                WarehouseName = c.Warehouse?.Name ?? "",
                CheckDate = c.CheckDate,
                Status = (int)c.Status,
                StatusName = GetCheckStatusName(c.Status),
                IsLocked = c.IsLocked,
                TotalProducts = c.Details.Count,
                CheckedCount = c.Details.Count(d => d.ActualQty.HasValue),
                TotalDiffAmount = c.TotalDiffAmount,
                Operator = c.Operator,
                CreateTime = c.CreateTime
            }).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CreateCheckAsync(StockCheckCreateDto dto, string currentUser)
    {
        var warehouse = await _uow.GetRepository<BaseWarehouse>().Query()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted)
            ?? throw new BusinessException("仓库不存在或已删除", "WAREHOUSE_NOT_FOUND");

        var stocks = await _uow.GetRepository<BaseProductStock>().Query()
            .Include(s => s.Product)
            .Where(s => s.WarehouseId == dto.WarehouseId && !s.IsDeleted && s.Quantity > 0)
            .ToListAsync();

        if (stocks.Count == 0)
            throw new BusinessException("该仓库没有可盘点的库存记录", "NO_STOCK");

        var check = new InvStockCheck
        {
            CheckNo = await _numberGen.GenerateWithTimeAsync("CHK", "Inv_StockCheck", "CheckNo"),
            WarehouseId = dto.WarehouseId,
            CheckDate = DateTime.Now,
            Status = StockCheckStatus.New,
            IsLocked = true,
            Operator = currentUser,
            Remark = dto.Remark,
            CreateTime = DateTime.Now,
            CreateBy = currentUser
        };

        foreach (var stock in stocks)
        {
            check.Details.Add(new InvStockCheckDetail
            {
                ProductId = stock.ProductId,
                SystemQty = stock.Quantity,
                ActualQty = null,
                DiffQty = 0,
                UnitPrice = stock.Product?.PurchasePrice ?? 0,
                CreateTime = DateTime.Now,
                CreateBy = currentUser
            });
        }

        await _uow.GetRepository<InvStockCheck>().AddAsync(check);
        await _uow.SaveChangesAsync();

        return check.Id;
    }

    public async Task<StockCheckDto> GetCheckDetailAsync(long checkId)
    {
        var check = await _uow.GetRepository<InvStockCheck>().Query()
            .Include(c => c.Warehouse)
            .Include(c => c.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(c => c.Id == checkId && !c.IsDeleted)
            ?? throw new BusinessException("盘点单不存在", "CHECK_NOT_FOUND");

        return new StockCheckDto
        {
            Id = check.Id,
            CheckNo = check.CheckNo,
            WarehouseId = check.WarehouseId,
            WarehouseName = check.Warehouse?.Name,
            CheckDate = check.CheckDate,
            Status = (int)check.Status,
            StatusName = GetCheckStatusName(check.Status),
            IsLocked = check.IsLocked,
            TotalDiffAmount = check.TotalDiffAmount,
            Approver = check.Approver,
            ApproveTime = check.ApproveTime,
            Operator = check.Operator,
            Remark = check.Remark,
            CreateTime = check.CreateTime,
            Details = check.Details.Select(d => new StockCheckDetailDto
            {
                Id = d.Id,
                StockCheckId = d.StockCheckId,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code,
                ProductName = d.Product?.Name,
                SystemQty = d.SystemQty,
                ActualQty = d.ActualQty,
                DiffQty = d.DiffQty,
                UnitPrice = d.UnitPrice,
                Remark = d.Remark
            }).ToList()
        };
    }

    public async Task RecordActualAsync(long checkId, List<StockCheckRecordDto> records, string user)
    {
        var check = await _uow.GetRepository<InvStockCheck>().Query()
            .Include(c => c.Details)
            .FirstOrDefaultAsync(c => c.Id == checkId && !c.IsDeleted)
            ?? throw new BusinessException("盘点单不存在", "CHECK_NOT_FOUND");

        if (check.Status != StockCheckStatus.New && check.Status != StockCheckStatus.Checking)
            throw new BusinessException($"当前状态【{GetCheckStatusName(check.Status)}】不允许录入实盘", "CHECK_STATUS_INVALID");

        var detailRepo = _uow.GetRepository<InvStockCheckDetail>();
        var detailDict = check.Details.ToDictionary(d => d.Id);

        foreach (var record in records)
        {
            if (!detailDict.TryGetValue(record.DetailId, out var detail))
                throw new BusinessException($"盘点明细ID {record.DetailId} 不存在", "DETAIL_NOT_FOUND");

            detail.ActualQty = record.ActualQty;
            detail.DiffQty = record.ActualQty - detail.SystemQty;
            detail.Remark = record.Remark;
            detail.UpdateTime = DateTime.Now;
            detail.UpdateBy = user;
            detailRepo.Update(detail);
        }

        // 首次录入时切换状态
        if (check.Status == StockCheckStatus.New)
        {
            check.Status = StockCheckStatus.Checking;
            check.UpdateTime = DateTime.Now;
            check.UpdateBy = user;
        }

        // 汇总差异金额
        var updatedDetails = check.Details;
        check.TotalDiffAmount = updatedDetails.Sum(d =>
            d.DiffQty != 0 && d.UnitPrice > 0 ? d.DiffQty * d.UnitPrice : 0);

        await _uow.SaveChangesAsync();
    }

    public async Task SubmitCheckAsync(long checkId, string user)
    {
        var check = await _uow.GetRepository<InvStockCheck>().Query()
            .Include(c => c.Details)
            .FirstOrDefaultAsync(c => c.Id == checkId && !c.IsDeleted)
            ?? throw new BusinessException("盘点单不存在", "CHECK_NOT_FOUND");

        if (check.Status != StockCheckStatus.Checking)
            throw new BusinessException($"当前状态【{GetCheckStatusName(check.Status)}】不允许提交审批", "CHECK_STATUS_INVALID");

        // 校验所有明细已录入
        if (check.Details.Any(d => !d.ActualQty.HasValue))
            throw new BusinessException("存在未录入实盘数量的明细，请完成后再提交", "CHECK_INCOMPLETE");

        check.Status = StockCheckStatus.PendingApproval;
        check.UpdateTime = DateTime.Now;
        check.UpdateBy = user;

        await _uow.SaveChangesAsync();
    }

    public async Task AdjustStockCheckAsync(long checkId, string approver)
    {
        using var tx = await _uow.BeginTransactionAsync();
        try
        {
            var check = await _uow.GetRepository<InvStockCheck>().Query()
                .Include(c => c.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(c => c.Id == checkId && !c.IsDeleted)
                ?? throw new BusinessException("盘点单不存在", "CHECK_NOT_FOUND");

            if (check.Status != StockCheckStatus.PendingApproval)
                throw new BusinessException($"当前状态【{GetCheckStatusName(check.Status)}】不允许审批", "CHECK_STATUS_INVALID");

            var stockRepo = _uow.GetRepository<BaseProductStock>();
            var txnRepo = _uow.StockTransactions;
            var detailRepo = _uow.GetRepository<InvStockCheckDetail>();

            var diffs = check.Details.Where(d => d.DiffQty != 0).ToList();
            foreach (var d in diffs)
            {
                var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(d.ProductId, check.WarehouseId)
                    ?? new BaseProductStock
                    {
                        ProductId = d.ProductId,
                        WarehouseId = check.WarehouseId,
                        Quantity = 0,
                        LockQty = 0,
                        CreateTime = DateTime.Now,
                        CreateBy = approver
                    };

                stock.Quantity += d.DiffQty;
                if (stock.Quantity < 0)
                    throw new BusinessException(
                        $"设备 [{d.Product?.Code ?? d.ProductId.ToString()}] 盘亏后库存为负（系统:{d.SystemQty}, 实盘:{d.ActualQty}），操作中止",
                        "STOCK_NEGATIVE");

                stock.UpdateTime = DateTime.Now;
                stock.UpdateBy = approver;

                if (stock.Id == 0)
                    await stockRepo.AddAsync(stock);
                else
                    stockRepo.Update(stock);

                var isGain = d.DiffQty > 0;
                await txnRepo.AddAsync(new InvStockTransaction
                {
                    TransactionNo = await _numberGen.GenerateWithTimeAsync("TXN", "Inv_StockTransaction", "TransactionNo"),
                    TransactionType = isGain ? TransactionType.StockGain : TransactionType.StockLoss,
                    ProductId = d.ProductId,
                    WarehouseId = check.WarehouseId,
                    Qty = d.DiffQty,
                    BalanceQty = stock.Quantity,
                    RefType = "StockCheck",
                    RefId = check.Id,
                    RefNo = check.CheckNo,
                    Operator = approver,
                    Remark = $"盘点{(isGain ? "盘盈" : "盘亏")}（系统:{d.SystemQty}, 实盘:{d.ActualQty}, 差异:{d.DiffQty}）",
                    CreateTime = DateTime.Now,
                    CreateBy = approver
                });
            }

            // 更新系统快照 + 解冻
            foreach (var d in check.Details)
            {
                d.SystemQty = d.ActualQty ?? d.SystemQty;
                d.UpdateTime = DateTime.Now;
                d.UpdateBy = approver;
                detailRepo.Update(d);
            }

            check.IsLocked = false;
            check.Status = StockCheckStatus.Approved;
            check.Approver = approver;
            check.ApproveTime = DateTime.Now;
            check.UpdateTime = DateTime.Now;
            check.UpdateBy = approver;

            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
            InvalidateStockCache();
        }
        catch (BusinessException)
        {
            await tx.RollbackAsync();
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new BusinessException("库存数据已被他人修改，请刷新后重试", "STOCK_CONFLICT");
        }
    }

    public async Task CancelCheckAsync(long checkId, string user)
    {
        var check = await _uow.GetRepository<InvStockCheck>().Query()
            .FirstOrDefaultAsync(c => c.Id == checkId && !c.IsDeleted)
            ?? throw new BusinessException("盘点单不存在", "CHECK_NOT_FOUND");

        if (check.Status == StockCheckStatus.Approved)
            throw new BusinessException("已审批的盘点单不能取消", "CHECK_STATUS_INVALID");
        if (check.Status == StockCheckStatus.Cancelled)
            throw new BusinessException("盘点单已取消", "CHECK_STATUS_INVALID");

        check.Status = StockCheckStatus.Cancelled;
        check.IsLocked = false;
        check.UpdateTime = DateTime.Now;
        check.UpdateBy = user;

        await _uow.SaveChangesAsync();
    }

    public async Task<bool> IsProductInActiveCheckAsync(long productId)
    {
        return await _uow.GetRepository<InvStockCheckDetail>().Query()
            .AnyAsync(d => d.ProductId == productId && !d.IsDeleted
                && d.StockCheck != null && d.StockCheck.IsLocked && !d.StockCheck.IsDeleted);
    }

    private static string GetCheckStatusName(StockCheckStatus s) => s switch
    {
        StockCheckStatus.New => "新建",
        StockCheckStatus.Checking => "盘点中",
        StockCheckStatus.PendingApproval => "待审批",
        StockCheckStatus.Approved => "已审批",
        StockCheckStatus.Cancelled => "已取消",
        _ => s.ToString()
    };

    // ==================== 调拨管理 ====================

    public async Task<PagedResult<StockTransferListDto>> GetTransferListAsync(StockTransferQueryDto query)
    {
        var repo = _uow.GetRepository<InvStockTransfer>();
        var q = repo.Query()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Details)
            .Where(t => !t.IsDeleted);

        if (query.Status.HasValue)
            q = q.Where(t => t.Status == (TransferStatus)query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(t => t.TransferNo.Contains(query.Keyword));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<StockTransferListDto>
        {
            Items = items.Select(t => new StockTransferListDto
            {
                Id = t.Id,
                TransferNo = t.TransferNo,
                FromWarehouseName = t.FromWarehouse?.Name ?? "",
                ToWarehouseName = t.ToWarehouse?.Name ?? "",
                TransferDate = t.TransferDate,
                Status = (int)t.Status,
                StatusName = GetTransferStatusName(t.Status),
                TotalProducts = t.Details.Count,
                TotalQty = t.Details.Sum(d => d.Qty),
                Operator = t.Operator,
                CreateTime = t.CreateTime
            }).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CreateTransferAsync(StockTransferCreateDto dto, string currentUser)
    {
        if (dto.FromWarehouseId <= 0 || dto.ToWarehouseId <= 0)
            throw new BusinessException("请选择调出仓库和调入仓库", "WAREHOUSE_REQUIRED");
        if (dto.FromWarehouseId == dto.ToWarehouseId)
            throw new BusinessException("调出仓库和调入仓库不能相同", "SAME_WAREHOUSE");
        if (dto.Items == null || dto.Items.Count == 0)
            throw new BusinessException("请添加调拨设备明细", "NO_ITEMS");

        var fromWarehouse = await _uow.GetRepository<BaseWarehouse>().Query()
            .FirstOrDefaultAsync(w => w.Id == dto.FromWarehouseId && !w.IsDeleted)
            ?? throw new BusinessException("调出仓库不存在", "FROM_WAREHOUSE_NOT_FOUND");

        var toWarehouse = await _uow.GetRepository<BaseWarehouse>().Query()
            .FirstOrDefaultAsync(w => w.Id == dto.ToWarehouseId && !w.IsDeleted)
            ?? throw new BusinessException("调入仓库不存在", "TO_WAREHOUSE_NOT_FOUND");

        var transfer = new InvStockTransfer
        {
            TransferNo = await _numberGen.GenerateWithTimeAsync("TRF", "Inv_StockTransfer", "TransferNo"),
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            TransferDate = DateTime.Now,
            Status = TransferStatus.Draft,
            Operator = currentUser,
            Remark = dto.Remark,
            CreateTime = DateTime.Now,
            CreateBy = currentUser
        };

        foreach (var item in dto.Items)
        {
            if (item.Qty <= 0)
                throw new BusinessException("调拨数量必须大于0", "INVALID_QTY");

            transfer.Details.Add(new InvStockTransferDetail
            {
                ProductId = item.ProductId,
                Qty = item.Qty,
                Remark = item.Remark,
                CreateTime = DateTime.Now,
                CreateBy = currentUser
            });
        }

        await _uow.GetRepository<InvStockTransfer>().AddAsync(transfer);
        await _uow.SaveChangesAsync();

        return transfer.Id;
    }

    public async Task<StockTransferDto> GetTransferDetailAsync(long transferId)
    {
        var transfer = await _uow.GetRepository<InvStockTransfer>().Query()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted)
            ?? throw new BusinessException("调拨单不存在", "TRANSFER_NOT_FOUND");

        return new StockTransferDto
        {
            Id = transfer.Id,
            TransferNo = transfer.TransferNo,
            FromWarehouseId = transfer.FromWarehouseId,
            FromWarehouseName = transfer.FromWarehouse?.Name,
            ToWarehouseId = transfer.ToWarehouseId,
            ToWarehouseName = transfer.ToWarehouse?.Name,
            TransferDate = transfer.TransferDate,
            Status = (int)transfer.Status,
            StatusName = GetTransferStatusName(transfer.Status),
            Approver = transfer.Approver,
            ApproveTime = transfer.ApproveTime,
            RejectReason = transfer.RejectReason,
            Operator = transfer.Operator,
            Remark = transfer.Remark,
            CreateTime = transfer.CreateTime,
            Details = transfer.Details.Select(d => new StockTransferDetailDto
            {
                Id = d.Id,
                TransferId = d.TransferId,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code,
                ProductName = d.Product?.Name,
                Qty = d.Qty,
                Remark = d.Remark
            }).ToList()
        };
    }

    public async Task SubmitTransferAsync(long transferId, string user)
    {
        var transfer = await _uow.GetRepository<InvStockTransfer>().Query()
            .Include(t => t.Details)
            .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted)
            ?? throw new BusinessException("调拨单不存在", "TRANSFER_NOT_FOUND");

        if (transfer.Status != TransferStatus.Draft && transfer.Status != TransferStatus.Rejected)
            throw new BusinessException($"当前状态【{GetTransferStatusName(transfer.Status)}】不允许提交审批", "TRANSFER_STATUS_INVALID");

        transfer.Status = TransferStatus.PendingApproval;
        transfer.UpdateTime = DateTime.Now;
        transfer.UpdateBy = user;

        await _uow.SaveChangesAsync();
    }

    public async Task ApproveTransferAsync(long transferId, string approver)
    {
        var transfer = await _uow.GetRepository<InvStockTransfer>().Query()
            .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted)
            ?? throw new BusinessException("调拨单不存在", "TRANSFER_NOT_FOUND");

        if (transfer.Status != TransferStatus.PendingApproval)
            throw new BusinessException($"当前状态【{GetTransferStatusName(transfer.Status)}】不允许审批", "TRANSFER_STATUS_INVALID");

        transfer.Status = TransferStatus.Approved;
        transfer.Approver = approver;
        transfer.ApproveTime = DateTime.Now;
        transfer.UpdateTime = DateTime.Now;
        transfer.UpdateBy = approver;

        await _uow.SaveChangesAsync();
    }

    public async Task RejectTransferAsync(long transferId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessException("驳回原因不能为空", "REJECT_REASON_REQUIRED");

        var transfer = await _uow.GetRepository<InvStockTransfer>().Query()
            .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted)
            ?? throw new BusinessException("调拨单不存在", "TRANSFER_NOT_FOUND");

        if (transfer.Status != TransferStatus.PendingApproval)
            throw new BusinessException($"当前状态【{GetTransferStatusName(transfer.Status)}】不允许驳回", "TRANSFER_STATUS_INVALID");

        transfer.Status = TransferStatus.Rejected;
        transfer.Approver = approver;
        transfer.ApproveTime = DateTime.Now;
        transfer.RejectReason = reason;
        transfer.UpdateTime = DateTime.Now;
        transfer.UpdateBy = approver;

        await _uow.SaveChangesAsync();
    }

    public async Task ExecuteTransferAsync(long transferId, string user)
    {
        using var tx = await _uow.BeginTransactionAsync();
        try
        {
            var transfer = await _uow.GetRepository<InvStockTransfer>().Query()
                .Include(t => t.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted)
                ?? throw new BusinessException("调拨单不存在", "TRANSFER_NOT_FOUND");

            if (transfer.Status != TransferStatus.Approved)
                throw new BusinessException($"当前状态【{GetTransferStatusName(transfer.Status)}】不允许执行调拨", "TRANSFER_STATUS_INVALID");

            // 校验调出仓库是否有正在盘点的设备
            var productIds = transfer.Details.Select(d => d.ProductId).ToList();
            var hasActiveCheck = await _uow.GetRepository<InvStockCheckDetail>().Query()
                .AnyAsync(d => productIds.Contains(d.ProductId) && !d.IsDeleted
                    && d.StockCheck != null && d.StockCheck.IsLocked && !d.StockCheck.IsDeleted);
            if (hasActiveCheck)
                throw new BusinessException("调出仓库中存在正在盘点的设备，无法执行调拨", "CHECK_IN_PROGRESS");

            var stockRepo = _uow.GetRepository<BaseProductStock>();
            var txnRepo = _uow.StockTransactions;

            foreach (var detail in transfer.Details)
            {
                // 调出仓库：校验可用库存 → 扣减
                var fromStock = await _uow.Stocks.FindByProductAndWarehouseAsync(detail.ProductId, transfer.FromWarehouseId)
                    ?? throw new BusinessException(
                        $"设备 [{detail.Product?.Code ?? detail.ProductId.ToString()}] 在调出仓库无库存记录", "NO_STOCK");

                var availableQty = fromStock.Quantity - fromStock.LockQty;
                if (availableQty < detail.Qty)
                    throw new BusinessException(
                        $"设备 [{detail.Product?.Code ?? detail.ProductId.ToString()}] 可用库存不足（可用:{availableQty}, 需要:{detail.Qty}）",
                        "STOCK_INSUFFICIENT");

                fromStock.Quantity -= detail.Qty;
                if (fromStock.Quantity < 0)
                    throw new BusinessException(
                        $"设备 [{detail.Product?.Code ?? detail.ProductId.ToString()}] 调拨后库存为负，操作中止",
                        "STOCK_NEGATIVE");
                fromStock.UpdateTime = DateTime.Now;
                fromStock.UpdateBy = user;
                stockRepo.Update(fromStock);

                await txnRepo.AddAsync(new InvStockTransaction
                {
                    TransactionNo = await _numberGen.GenerateWithTimeAsync("TXN", "Inv_StockTransaction", "TransactionNo"),
                    TransactionType = TransactionType.TransferOut,
                    ProductId = detail.ProductId,
                    WarehouseId = transfer.FromWarehouseId,
                    Qty = -detail.Qty,
                    BalanceQty = fromStock.Quantity,
                    RefType = "StockTransfer",
                    RefId = transfer.Id,
                    RefNo = transfer.TransferNo,
                    Operator = user,
                    Remark = $"调拨至 [{transfer.ToWarehouse?.Name ?? transfer.ToWarehouseId.ToString()}]",
                    CreateTime = DateTime.Now,
                    CreateBy = user
                });

                // 调入仓库：查找或创建库存 → 增加
                var toStock = await _uow.Stocks.FindByProductAndWarehouseAsync(detail.ProductId, transfer.ToWarehouseId)
                    ?? new BaseProductStock
                    {
                        ProductId = detail.ProductId,
                        WarehouseId = transfer.ToWarehouseId,
                        Quantity = 0,
                        LockQty = 0,
                        CreateTime = DateTime.Now,
                        CreateBy = user
                    };

                toStock.Quantity += detail.Qty;
                toStock.UpdateTime = DateTime.Now;
                toStock.UpdateBy = user;

                if (toStock.Id == 0)
                    await stockRepo.AddAsync(toStock);
                else
                    stockRepo.Update(toStock);

                await txnRepo.AddAsync(new InvStockTransaction
                {
                    TransactionNo = await _numberGen.GenerateWithTimeAsync("TXN", "Inv_StockTransaction", "TransactionNo"),
                    TransactionType = TransactionType.TransferIn,
                    ProductId = detail.ProductId,
                    WarehouseId = transfer.ToWarehouseId,
                    Qty = detail.Qty,
                    BalanceQty = toStock.Quantity,
                    RefType = "StockTransfer",
                    RefId = transfer.Id,
                    RefNo = transfer.TransferNo,
                    Operator = user,
                    Remark = $"从 [{transfer.FromWarehouse?.Name ?? transfer.FromWarehouseId.ToString()}] 调入",
                    CreateTime = DateTime.Now,
                    CreateBy = user
                });
            }

            transfer.Status = TransferStatus.Completed;
            transfer.UpdateTime = DateTime.Now;
            transfer.UpdateBy = user;

            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
            InvalidateStockCache();
        }
        catch (BusinessException)
        {
            await tx.RollbackAsync();
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new BusinessException("库存数据已被他人修改，请刷新后重试", "STOCK_CONFLICT");
        }
    }

    public async Task CancelTransferAsync(long transferId, string user)
    {
        var transfer = await _uow.GetRepository<InvStockTransfer>().Query()
            .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted)
            ?? throw new BusinessException("调拨单不存在", "TRANSFER_NOT_FOUND");

        if (transfer.Status == TransferStatus.Completed)
            throw new BusinessException("已完成的调拨单不能取消", "TRANSFER_STATUS_INVALID");
        if (transfer.Status == TransferStatus.Cancelled)
            throw new BusinessException("调拨单已取消", "TRANSFER_STATUS_INVALID");

        transfer.Status = TransferStatus.Cancelled;
        transfer.UpdateTime = DateTime.Now;
        transfer.UpdateBy = user;

        await _uow.SaveChangesAsync();
    }

    public async Task<List<StockTransferProductDto>> GetTransferProductsAsync(long warehouseId)
    {
        return await _uow.GetRepository<BaseProductStock>().Query()
            .Include(s => s.Product)
            .Where(s => s.WarehouseId == warehouseId && !s.IsDeleted && s.Quantity > 0)
            .Select(s => new StockTransferProductDto
            {
                ProductId = s.ProductId,
                ProductCode = s.Product!.Code,
                ProductName = s.Product!.Name,
                AvailableQty = s.Quantity - s.LockQty,
                UnitPrice = s.Product!.PurchasePrice
            })
            .ToListAsync();
    }

    private static string GetTransferStatusName(TransferStatus s) => s switch
    {
        TransferStatus.Draft => "草稿",
        TransferStatus.PendingApproval => "待审批",
        TransferStatus.Approved => "已审批",
        TransferStatus.Completed => "已完成",
        TransferStatus.Cancelled => "已取消",
        TransferStatus.Rejected => "已驳回",
        _ => s.ToString()
    };

    // ==================== 低库存预警 ====================

    public async Task<List<StockAlertWarningDto>> GetWarningsAsync()
    {
        return await (
            from p in _db.BaseProducts
            join stock in _db.BaseProductStocks on p.Id equals stock.ProductId into stockJoin
            from st in stockJoin.Where(s => !s.IsDeleted).DefaultIfEmpty()
            join w in _db.BaseWarehouses on (st != null ? st.WarehouseId : 0) equals w.Id into whJoin
            from wh in whJoin.Where(w => !w.IsDeleted).DefaultIfEmpty()
            where !p.IsDeleted && p.MinStock > 0
                  && (st == null || st.Quantity <= p.MinStock)
            select new StockAlertWarningDto
            {
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                Spec = p.Spec,
                WarehouseId = st != null ? st.WarehouseId : 0,
                WarehouseName = wh != null ? wh.Name : "",
                CurrentQty = st != null ? st.Quantity : 0,
                AvailableQty = st != null ? st.Quantity - st.LockQty : 0,
                MinStock = p.MinStock,
                AlertLevel = (st == null || st.Quantity <= 0) ? (int)AlertLevel.OutOfStock : (int)AlertLevel.LowStock,
                AlertLevelName = (st == null || st.Quantity <= 0) ? "缺货" : "低库存"
            }).ToListAsync();
    }

    public async Task<PagedResult<StockAlertRecordDto>> GetAlertRecordsAsync(StockAlertQueryDto query)
    {
        var q = from a in _db.InvStockAlerts
                join p in _db.BaseProducts on a.ProductId equals p.Id
                join w in _db.BaseWarehouses on a.WarehouseId equals w.Id
                select new StockAlertRecordDto
                {
                    Id = a.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    Spec = p.Spec,
                    WarehouseName = w.Name,
                    AlertLevel = (int)a.AlertLevel,
                    AlertLevelName = a.AlertLevel == AlertLevel.OutOfStock ? "缺货" : "低库存",
                    CurrentQty = a.CurrentQty,
                    MinStock = a.MinStock,
                    IsResolved = a.IsResolved,
                    ResolvedTime = a.ResolvedTime,
                    CreateTime = a.CreateTime
                };

        if (query.IsResolved.HasValue)
            q = q.Where(a => a.IsResolved == query.IsResolved.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<StockAlertRecordDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<StockWarningStatDto> GetWarningStatsAsync()
    {
        var warnings = await GetWarningsAsync();
        var totalProducts = await _db.BaseProducts.CountAsync(p => !p.IsDeleted);

        return new StockWarningStatDto
        {
            OutOfStockCount = warnings.Count(w => w.AlertLevel == (int)AlertLevel.OutOfStock),
            LowStockCount = warnings.Count(w => w.AlertLevel == (int)AlertLevel.LowStock),
            NormalCount = totalProducts - warnings.Count,
            TotalProductCount = totalProducts
        };
    }

    public async Task<List<CategoryWarningDto>> GetCategoryWarningsAsync()
    {
        var warnings = await GetWarningsAsync();

        // 查询分类名称（通过 ProductId 批量查）
        var productIds = warnings.Select(w => w.ProductId).Distinct().ToList();
        var categoryMap = await _db.BaseProducts
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, CategoryName = p.Category != null ? p.Category.Name : "未分类" })
            .ToDictionaryAsync(x => x.Id, x => x.CategoryName);

        return warnings
            .GroupBy(w => categoryMap.TryGetValue(w.ProductId, out var cn) ? cn : "未分类")
            .Select(g => new CategoryWarningDto
            {
                CategoryName = g.Key,
                OutOfStockCount = g.Count(w => w.AlertLevel == (int)AlertLevel.OutOfStock),
                LowStockCount = g.Count(w => w.AlertLevel == (int)AlertLevel.LowStock),
                TotalCount = g.Count()
            })
            .OrderByDescending(c => c.TotalCount)
            .Take(10)
            .ToList();
    }

    public async Task CheckAndCreateAlertsAsync()
    {
        var warnings = await GetWarningsAsync();
        if (warnings.Count == 0)
        {
            _logger.LogInformation("库存预警检查完成，无预警设备");
            return;
        }

        var unresolvedAlerts = await _db.InvStockAlerts
            .Where(a => !a.IsResolved)
            .ToListAsync();

        var newAlertCount = 0;
        var resolvedCount = 0;

        foreach (var w in warnings)
        {
            var existingAlert = unresolvedAlerts
                .FirstOrDefault(a => a.ProductId == w.ProductId && a.WarehouseId == w.WarehouseId);

            if (existingAlert == null)
            {
                var alert = new InvStockAlert
                {
                    ProductId = w.ProductId,
                    WarehouseId = w.WarehouseId,
                    AlertLevel = (AlertLevel)w.AlertLevel,
                    CurrentQty = w.CurrentQty,
                    MinStock = w.MinStock,
                    IsResolved = false,
                    CreateTime = DateTime.Now
                };
                _db.InvStockAlerts.Add(alert);
                newAlertCount++;

                // 发送站内信通知仓库管理员和经理
                await NotifyAlertAsync(alert, w.ProductName, w.WarehouseName);
            }
            else if (existingAlert.CurrentQty != w.CurrentQty)
            {
                existingAlert.CurrentQty = w.CurrentQty;
                existingAlert.AlertLevel = (AlertLevel)w.AlertLevel;
            }
        }

        // 补货后自动标记预警已解决
        foreach (var alert in unresolvedAlerts)
        {
            var stillWarning = warnings.Any(w => w.ProductId == alert.ProductId && w.WarehouseId == alert.WarehouseId);
            if (!stillWarning)
            {
                alert.IsResolved = true;
                alert.ResolvedTime = DateTime.Now;
                resolvedCount++;
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("库存预警检查完成: 新增预警 {NewCount} 条, 已解决 {ResolvedCount} 条", newAlertCount, resolvedCount);
    }

    private async Task NotifyAlertAsync(InvStockAlert alert, string productName, string warehouseName)
    {
        try
        {
            var levelText = alert.AlertLevel == AlertLevel.OutOfStock ? "缺货" : "低库存";
            var title = $"【库存预警】{productName} - {levelText}";
            var content = $"仓库 [{warehouseName}] 中的设备 [{productName}] 库存不足。\n"
                        + $"当前库存: {alert.CurrentQty}，安全库存: {alert.MinStock}，预警级别: {levelText}。\n"
                        + $"请及时安排补货。";

            // 通知仓库管理员
            var warehouse = await _uow.GetRepository<BaseWarehouse>().Query()
                .FirstOrDefaultAsync(w => w.Id == alert.WarehouseId && !w.IsDeleted);
            if (warehouse?.Manager != null)
            {
                var managers = await _uow.GetRepository<SysUser>().Query()
                    .Where(u => u.IsActive && !u.IsDeleted && u.RealName == warehouse.Manager)
                    .ToListAsync();
                foreach (var m in managers)
                    await _notification.SendAsync(m.Id, title, content, NotificationType.StockAlert, "StockAlert", alert.Id);
            }

            // 通知所有经理角色用户
            var managerRoleIds = await _db.SysRoles
                .Where(r => r.RoleCode == "manager" && !r.IsDeleted)
                .Select(r => r.Id)
                .ToListAsync();
            if (managerRoleIds.Count > 0)
            {
                var notifiedIds = await _db.SysUserRoles
                    .Where(ur => managerRoleIds.Contains(ur.RoleId))
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                var allManagers = await _uow.GetRepository<SysUser>().Query()
                    .Where(u => notifiedIds.Contains(u.Id) && u.IsActive && !u.IsDeleted)
                    .ToListAsync();
                foreach (var m in allManagers)
                    await _notification.SendAsync(m.Id, title, content, NotificationType.StockAlert, "StockAlert", alert.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送库存预警通知失败 ProductId={ProductId} WarehouseId={WarehouseId}", alert.ProductId, alert.WarehouseId);
        }
    }
}
