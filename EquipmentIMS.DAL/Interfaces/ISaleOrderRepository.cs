using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 销售订单仓储接口
/// </summary>
public interface ISaleOrderRepository
{
    Task<SaleSaleOrder?> GetByIdWithDetailsAsync(long id);
    Task<SaleSaleOrder?> GetByIdAsync(long id);
    void Update(SaleSaleOrder order);
}
