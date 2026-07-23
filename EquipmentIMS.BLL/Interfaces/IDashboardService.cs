using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(long userId, bool isManager);
    Task<SaleTrendData> GetSaleTrendAsync();
    Task<StockDistData> GetStockDistAsync();
    Task<List<PendingApprovalItem>> GetPendingApprovalsAsync();
}
