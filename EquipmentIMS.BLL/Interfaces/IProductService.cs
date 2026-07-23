using EquipmentIMS.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace EquipmentIMS.BLL.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, string? keyword = null, long? categoryId = null);
    Task<List<ProductDto>> GetAllAsync(string? keyword = null);
    Task<ProductDto?> GetByIdAsync(long id);
    Task<ProductDto> CreateAsync(ProductDto dto);
    Task UpdateAsync(ProductDto dto);
    Task DeleteAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
    Task<ImportResult> ImportExcelAsync(IFormFile file);
    /// <summary>根据条码查找设备</summary>
    Task<ProductDto?> FindByBarCodeAsync(string barCode);
}
