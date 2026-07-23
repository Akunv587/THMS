using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IWarehouseService
{
    Task<PagedResult<WarehouseDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);
    Task<List<WarehouseDto>> GetAllAsync();
    Task<WarehouseDto?> GetByIdAsync(long id);
    Task<WarehouseDto> CreateAsync(WarehouseDto dto);
    Task UpdateAsync(WarehouseDto dto);
    Task DeleteAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
}
