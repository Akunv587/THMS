using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IDeviceArchiveService
{
    Task<PagedResult<DeviceArchiveListDto>> GetListAsync(string? serialNo, long? customerId, int? status, int page, int pageSize);
    Task<DeviceArchiveDto?> GetByIdAsync(long id);
    Task<DeviceArchiveDto?> FindBySerialNoAsync(string serialNo);
    Task<long> CreateAsync(DeviceArchiveDto dto, string currentUser);
    Task UpdateAsync(DeviceArchiveDto dto, string currentUser);
    Task DeleteAsync(long id);
    Task<List<RepairOrderDto>> GetRepairOrdersAsync(long deviceArchiveId);
}
