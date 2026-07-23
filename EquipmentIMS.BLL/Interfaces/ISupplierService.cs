using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);
    Task<List<SupplierDto>> GetAllAsync(string? keyword = null);
    Task<SupplierDto?> GetByIdAsync(long id);
    Task<SupplierDto> CreateAsync(SupplierDto dto);
    Task UpdateAsync(SupplierDto dto);
    Task DeleteAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
}
