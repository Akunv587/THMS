using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryTreeDto>> GetTreeAsync();
    Task<CategoryDto?> GetByIdAsync(long id);
    Task<CategoryDto> CreateAsync(CategoryDto dto);
    Task UpdateAsync(CategoryDto dto);
    Task DeleteAsync(long id);
    Task<bool> HasChildrenAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
}
