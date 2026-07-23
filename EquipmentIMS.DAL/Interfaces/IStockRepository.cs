using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 库存仓储接口
/// </summary>
public interface IStockRepository
{
    Task<BaseProductStock?> FindByProductAndWarehouseAsync(long productId, long warehouseId);
}
