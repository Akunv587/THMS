using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IMaintenanceService
{
    Task<PagedResult<RepairOrderListDto>> GetListAsync(long? technicianId, int? status, long? deviceArchiveId, long currentUserId, bool isManager, int page, int pageSize);
    Task<RepairOrderDto?> GetByIdAsync(long id);
    Task<long> CreateAsync(RepairOrderDto dto, string currentUser);
    Task DispatchAsync(long orderId, long technicianId, string dispatcher);
    Task StartRepairAsync(long orderId, string technician);
    Task CompleteRepairAsync(long orderId, string repairDesc, decimal workHours, decimal repairCost, string technician);
    Task ConfirmAsync(long orderId, string confirmer);
    Task CloseAsync(long orderId, string user);
    Task CancelAsync(long orderId, string user);
    Task RecordRepairPartAsync(long repairOrderId, List<RepairPartDto> parts, string currentUser);
    Task<List<RepairPartDto>> GetRepairPartsAsync(long repairOrderId);

    // 保养计划
    Task<PagedResult<MaintenancePlanListDto>> GetPlansAsync(long? deviceArchiveId, int page, int pageSize);
    Task<List<MaintenancePlanDto>> GetPlansByDeviceAsync(long deviceId);
    Task<MaintenancePlanDto?> GetPlanByIdAsync(long id);
    Task<long> CreatePlanAsync(MaintenancePlanDto dto, string currentUser);
    Task UpdatePlanAsync(MaintenancePlanDto dto, string currentUser);
    Task DeletePlanAsync(long id);
    Task<List<MaintenancePlanDto>> GetDuePlansAsync();
}
