using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(int page, int pageSize, long userId, bool isManager, string? keyword = null);
    Task<List<CustomerDto>> GetAllAsync(long userId, bool isManager, string? keyword = null);
    Task<CustomerDto?> GetByIdAsync(long id);
    Task<CustomerDto> CreateAsync(CustomerDto dto);
    Task UpdateAsync(CustomerDto dto);
    Task DeleteAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
    Task<List<CustomerPriceDto>> GetPricesByCustomerAsync(long customerId);
    Task SetPriceAsync(CustomerPriceDto dto);
    Task UpdatePriceAsync(CustomerPriceDto dto);
    Task DeletePriceAsync(long priceId);
}
