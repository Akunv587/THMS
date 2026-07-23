using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.DTOs.Reports;

namespace EquipmentIMS.BLL.Interfaces;

public interface IReportService
{
    // 库存报表
    Task<List<InventoryReportDto>> GetInventorySummaryAsync(long? warehouseId, long? categoryId);
    Task<List<StockFundDto>> GetStockFundAnalysisAsync();
    Task<PagedResult<StockTransactionDto>> GetStockTransactionsAsync(StockTransactionQueryDto query);
    Task<List<StockWarningDto>> GetStockWarningsAsync();

    // 销售报表
    Task<List<SaleRankingDto>> GetSaleRankingAsync(DateTime startDate, DateTime endDate, string rankBy);
    Task<List<SaleTrendDto>> GetSaleTrendAsync(DateTime startDate, DateTime endDate, string groupBy);
    Task<SaleReturnStatDto> GetSaleReturnStatAsync(DateTime startDate, DateTime endDate);

    // 采购报表
    Task<List<PurchaseSummaryDto>> GetPurchaseSummaryAsync(DateTime startDate, DateTime endDate, string groupBy);
    Task<PurchaseReturnStatDto> GetPurchaseReturnStatAsync(DateTime startDate, DateTime endDate);

    // 维修报表
    Task<List<RepairStatDto>> GetRepairStatAsync(DateTime startDate, DateTime endDate, string statBy);
    Task<RepairCostAnalysisDto> GetRepairCostAnalysisAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetWarrantyRepairRateAsync(DateTime startDate, DateTime endDate);
    Task<List<PartsUsageDto>> GetPartsUsageStatAsync(DateTime startDate, DateTime endDate);
}
