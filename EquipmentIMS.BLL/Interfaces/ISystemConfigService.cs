using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Interfaces;

/// <summary>
/// 系统配置服务接口
/// </summary>
public interface ISystemConfigService
{
    Task<string?> GetAsync(string key);
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync(string key, string value);
    Task<List<SysConfig>> GetAllAsync();
    Task<List<SystemConfigDto>> GetAllForEditAsync();
    Task UpdateAsync(List<SystemConfigDto> configs);
}
