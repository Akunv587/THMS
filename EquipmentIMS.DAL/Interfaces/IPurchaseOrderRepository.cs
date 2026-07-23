using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 采购订单仓储接口
/// </summary>
public interface IPurchaseOrderRepository
{
    Task<PurPurchaseOrder?> GetByIdWithDetailsAsync(long id);
    Task<PurPurchaseOrder?> GetByIdAsync(long id);
    void Update(PurPurchaseOrder order);
}
