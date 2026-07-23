using EquipmentIMS.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace EquipmentIMS.BLL.Interfaces;

public interface IStockService
{
    /// <summary>库存分页查询（含缓存）</summary>
    Task<PagedResult<StockListDto>> GetStockListAsync(StockQueryDto query);

    /// <summary>库存分页查询（旧版兼容）</summary>
    Task<PagedResult<StockQueryDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);

    /// <summary>库存流水查询</summary>
    Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(StockTransactionQueryDto query);

    /// <summary>同步导出（小数据量）</summary>
    Task<byte[]> ExportTransactionsAsync(StockTransactionQueryDto query);

    /// <summary>异步导出（大数据量）</summary>
    Task<Guid> StartExportTaskAsync(StockTransactionQueryDto query, long userId);

    /// <summary>获取导出文件路径</summary>
    Task<string?> GetExportFilePathAsync(Guid taskId);

    Task<InitialImportPreviewDto> PreviewAsync(IFormFile file);
    Task<InitialImportResultDto> ImportAsync(List<InitialStockDto> items, string currentUser);

    /// <summary>获取分类列表（供筛选下拉）</summary>
    Task<List<CategorySimpleDto>> GetCategoriesAsync();

    /// <summary>获取仓库列表（供筛选下拉）</summary>
    Task<List<WarehouseSimpleDto>> GetWarehousesAsync();

    /// <summary>清除库存缓存（库存变更后调用）</summary>
    void InvalidateStockCache();

    // ==================== 盘点管理 ====================

    /// <summary>盘点单分页查询</summary>
    Task<PagedResult<StockCheckListDto>> GetCheckListAsync(StockCheckQueryDto query);

    /// <summary>新建盘点单（生成快照 + 冻结设备）</summary>
    Task<long> CreateCheckAsync(StockCheckCreateDto dto, string currentUser);

    /// <summary>获取盘点详情（含明细）</summary>
    Task<StockCheckDto> GetCheckDetailAsync(long checkId);

    /// <summary>录入实盘数量</summary>
    Task RecordActualAsync(long checkId, List<StockCheckRecordDto> records, string user);

    /// <summary>提交审批</summary>
    Task SubmitCheckAsync(long checkId, string user);

    /// <summary>差异调整（审批通过后执行库存调整）</summary>
    Task AdjustStockCheckAsync(long checkId, string approver);

    /// <summary>取消盘点</summary>
    Task CancelCheckAsync(long checkId, string user);

    /// <summary>检查产品是否处于活跃盘点中（供出入库拦截）</summary>
    Task<bool> IsProductInActiveCheckAsync(long productId);

    // ==================== 调拨管理 ====================

    /// <summary>调拨单分页查询</summary>
    Task<PagedResult<StockTransferListDto>> GetTransferListAsync(StockTransferQueryDto query);

    /// <summary>新建调拨单（草稿）</summary>
    Task<long> CreateTransferAsync(StockTransferCreateDto dto, string currentUser);

    /// <summary>获取调拨单详情（含明细）</summary>
    Task<StockTransferDto> GetTransferDetailAsync(long transferId);

    /// <summary>提交审批：草稿 → 待审批</summary>
    Task SubmitTransferAsync(long transferId, string user);

    /// <summary>审批通过：待审批 → 已审批</summary>
    Task ApproveTransferAsync(long transferId, string approver);

    /// <summary>驳回：待审批 → 已驳回</summary>
    Task RejectTransferAsync(long transferId, string approver, string reason);

    /// <summary>执行调拨：已审批 → 已完成（文档 §4.7.6）</summary>
    Task ExecuteTransferAsync(long transferId, string user);

    /// <summary>取消调拨</summary>
    Task CancelTransferAsync(long transferId, string user);

    /// <summary>获取调出仓库可调拨设备列表</summary>
    Task<List<StockTransferProductDto>> GetTransferProductsAsync(long warehouseId);

    // ==================== 低库存预警 ====================

    /// <summary>查询低库存预警（v_StockWarning 视图逻辑）</summary>
    Task<List<StockAlertWarningDto>> GetWarningsAsync();

    /// <summary>预警历史记录分页查询</summary>
    Task<PagedResult<StockAlertRecordDto>> GetAlertRecordsAsync(StockAlertQueryDto query);

    /// <summary>后台检查库存并创建/解决预警（由 StockAlertBackgroundService 调用）</summary>
    Task CheckAndCreateAlertsAsync();

    /// <summary>获取预警统计数据（缺货/低库存/正常/总数）</summary>
    Task<StockWarningStatDto> GetWarningStatsAsync();

    /// <summary>按设备分类统计预警分布（供柱状图）</summary>
    Task<List<CategoryWarningDto>> GetCategoryWarningsAsync();
}

public class CategorySimpleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class WarehouseSimpleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
