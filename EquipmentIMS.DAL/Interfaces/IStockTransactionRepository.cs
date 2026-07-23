using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 库存流水仓储接口
/// </summary>
public interface IStockTransactionRepository
{
    Task AddAsync(InvStockTransaction transaction);
    Task<IEnumerable<InvStockTransaction>> QueryAsync(DateTime? startTime, DateTime? endTime,
        long? productId, long? warehouseId, TransactionType? type, int page, int pageSize);
    Task<int> CountAsync(DateTime? startTime, DateTime? endTime,
        long? productId, long? warehouseId, TransactionType? type);
}
